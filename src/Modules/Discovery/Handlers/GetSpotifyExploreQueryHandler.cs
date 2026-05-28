using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicRec.Catalog.Entities;
using MusicRec.Discovery.DTOs;
using MusicRec.Discovery.Queries;
using MusicRec.Infrastructure;
using MusicRec.Spotify;
using MusicRec.Spotify.Models;

namespace MusicRec.Discovery.Handlers;

/// <summary>
/// Spotify 实时探索推荐处理器 — 随机流派关键词 + 随机偏移搜索，结果自动导入本地库
/// </summary>
/// <remarks>
/// 流程：
///   1. 随机选取 1 个流派关键词
///   2. 随机 offset 调用 Spotify Search API（避免每次都返回相同结果）
///   3. Cache-Aside 导入：已存在的曲目跳过，新曲目写入本地库（含 Artist、Album）
///   4. 返回含本地 Id 的 DiscoveryResultDto（支持收藏和详情页跳转）
///
/// 不使用缓存 — 每次请求都是全新随机搜索，确保"换一批"始终有效。
///
/// Spotify Recommendations API（/v1/recommendations）在开发模式 App 下不可用，
/// 因此使用 Search API + 随机偏移作为主要策略，结果在导入后也能被推荐算法复用。
/// </remarks>
public class GetSpotifyExploreQueryHandler : IRequestHandler<GetSpotifyExploreQuery, IReadOnlyList<DiscoveryResultDto>>
{
    private readonly MusicRecDbContext _db;
    private readonly ISpotifyClient _spotify;
    private readonly ILogger<GetSpotifyExploreQueryHandler> _logger;

    // 用随机字母（a-z）+ 年份/随机词保证每次都能搜到结果
    // Spotify 搜索对单个字母几乎总是返回大量曲目
    private static readonly string[] SearchSeeds =
        ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
         "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
         "the", "love", "life", "night", "dream", "star", "heart", "fire",
         "one", "time", "world", "light", "dark", "gone", "home", "rain"];

    public GetSpotifyExploreQueryHandler(MusicRecDbContext db, ISpotifyClient spotify,
        ILogger<GetSpotifyExploreQueryHandler> logger)
    {
        _db = db;
        _spotify = spotify;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscoveryResultDto>> Handle(
        GetSpotifyExploreQuery request, CancellationToken ct)
    {
        // ── 阶段 1：最多重试 3 次，每次用不同随机关键词 ────
        // Spotify API 在开发模式 App 下可能间歇性返回 400，
        // 多关键词重试大幅提高成功率

        var usedKeywords = new HashSet<string>();
        const int maxAttempts = 3;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 随机选取搜索关键词，避开已失败的
            var available = SearchSeeds.Where(k => !usedKeywords.Contains(k)).ToList();
            if (available.Count == 0) break;

            var keyword = available[Random.Shared.Next(available.Count)];
            usedKeywords.Add(keyword);
            var offset = Random.Shared.Next(0, 200); // 更大的随机偏移增加多样性

            // Spotify 搜索
            _logger.LogInformation("探索尝试 #{Attempt}: keyword={Keyword}, offset={Offset}", attempt + 1, keyword, offset);
            SearchResponse searchResponse;
            try
            {
                searchResponse = await _spotify.SearchAsync(keyword, "track", request.Limit, offset, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "探索尝试 #{Attempt} 异常: keyword={Keyword}", attempt + 1, keyword);
                continue; // 失败 → 下一个关键词
            }

            var tracks = searchResponse.Tracks?.Items;
            if (tracks is null || tracks.Count == 0)
            {
                _logger.LogWarning("探索尝试 #{Attempt} 返回空: keyword={Keyword}", attempt + 1, keyword);
                continue; // 空结果 → 下一个关键词
            }

            _logger.LogInformation("探索尝试 #{Attempt} 成功: keyword={Keyword}, count={Count}", attempt + 1, keyword, tracks.Count);

            // ── 阶段 2：Cache-Aside 导入曲目 ──────────────
            var existingIds = await _db.Set<Track>()
                .Where(t => tracks.Select(tr => tr.Id).Contains(t.SpotifyTrackId))
                .ToDictionaryAsync(t => t.SpotifyTrackId, ct);

            var importedTracks = new List<Track>(tracks.Count);

            foreach (var trackObj in tracks)
            {
                if (existingIds.TryGetValue(trackObj.Id, out var existing))
                {
                    importedTracks.Add(existing);
                    continue;
                }

                if (trackObj.Album is null)
                    continue;

                var imported = await ImportTrackAsync(trackObj, ct);
                if (imported is not null)
                    importedTracks.Add(imported);
            }

            if (importedTracks.Count == 0)
                continue; // 导入全部失败 → 下一个关键词

            // ── 阶段 3：加载导航属性 + 映射 DTO ────────────
            var trackIds = importedTracks.Select(t => t.Id).ToList();
            var loadedTracks = await _db.Set<Track>()
                .Include(t => t.TrackArtists!).ThenInclude(ta => ta.Artist)
                .Include(t => t.TrackGenres!).ThenInclude(tg => tg.Genre)
                .Include(t => t.Album)
                .AsNoTracking()
                .Where(t => trackIds.Contains(t.Id))
                .ToListAsync(ct);

            return loadedTracks.Select(t =>
            {
                var artistSummary = t.TrackArtists is { Count: > 0 }
                    ? string.Join(", ", t.TrackArtists.Select(ta => ta.Artist?.Name ?? "未知"))
                    : "未知艺术家";

                var genres = t.TrackGenres?
                    .Select(tg => tg.Genre?.Name)
                    .Where(g => g is not null)
                    .Select(g => g!)
                    .ToList() ?? new List<string>();

                return new DiscoveryResultDto(
                    Id: t.Id,
                    Name: t.Name,
                    CoverImageUrl: t.CoverImageUrl,
                    DurationMs: t.DurationMs,
                    Popularity: t.Popularity,
                    ArtistsSummary: artistSummary,
                    AlbumName: t.Album?.Name,
                    DiscoveryReason: $"发现 · {keyword}",
                    Genres: genres
                );
            }).ToList();
        }

        // 3 次重试全部失败
        _logger.LogError("探索推荐全部 {MaxAttempts} 次尝试均失败", maxAttempts);
        return Array.Empty<DiscoveryResultDto>();
    }

    /// <summary>
    /// Cache-Aside 导入单首曲目（含 Artist、Album）
    /// </summary>
    private async Task<Track?> ImportTrackAsync(TrackObject trackObj, CancellationToken ct)
    {
        var album = await _db.Set<Album>()
            .FirstOrDefaultAsync(a => a.SpotifyAlbumId == trackObj.Album!.Id, ct);
        if (album is null)
        {
            album = new Album
            {
                Id = Guid.NewGuid(),
                SpotifyAlbumId = trackObj.Album!.Id,
                Name = trackObj.Album.Name,
                ReleaseDate = trackObj.Album.ReleaseDate,
                CoverImageUrl = trackObj.Album.Images?.FirstOrDefault()?.Url,
                AlbumType = trackObj.Album.AlbumType,
                TotalTracks = trackObj.Album.TotalTracks
            };
            _db.Set<Album>().Add(album);
        }

        var artistObjs = trackObj.Artists ?? new List<SimplifiedArtistObject>();
        var artists = new List<Artist>();
        foreach (var aObj in artistObjs)
        {
            var artist = await _db.Set<Artist>()
                .FirstOrDefaultAsync(ar => ar.SpotifyArtistId == aObj.Id, ct);
            if (artist is null)
            {
                artist = new Artist
                {
                    Id = Guid.NewGuid(),
                    SpotifyArtistId = aObj.Id,
                    Name = aObj.Name
                };
                _db.Set<Artist>().Add(artist);
            }
            artists.Add(artist);
        }

        var track = new Track
        {
            Id = Guid.NewGuid(),
            SpotifyTrackId = trackObj.Id,
            Name = trackObj.Name,
            AlbumId = album.Id,
            DurationMs = trackObj.DurationMs,
            Popularity = trackObj.Popularity,
            ReleaseDate = trackObj.Album?.ReleaseDate,
            CoverImageUrl = trackObj.Album?.Images?.FirstOrDefault()?.Url
        };
        _db.Set<Track>().Add(track);

        foreach (var artist in artists)
            _db.Set<TrackArtist>().Add(new TrackArtist { TrackId = track.Id, ArtistId = artist.Id });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            var existing = await _db.Set<Track>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.SpotifyTrackId == trackObj.Id, ct);
            return existing;
        }

        return track;
    }
}
