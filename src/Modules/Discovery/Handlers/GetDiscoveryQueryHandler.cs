using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicRec.Catalog.Entities;
using MusicRec.Discovery.DTOs;
using MusicRec.Discovery.Queries;
using MusicRec.Discovery.Services;
using MusicRec.Infrastructure;
using MusicRec.Shared;
using MusicRec.Shared.Caching;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.Discovery.Handlers;

/// <summary>
/// 个性化探索推荐处理器 — 结合用户画像做 70/20/10 探索分配
/// </summary>
/// <remarks>
/// 70%：热门高匹配曲目
/// 20%：用户偏好流派内的曲目
/// 10%：完全随机的全新风格
///
/// 依赖用户画像（UserProfile），但无画像时降级为纯冷启动。
/// 候选池限制 200 首。
/// </remarks>
public class GetDiscoveryQueryHandler : IRequestHandler<GetDiscoveryQuery, IReadOnlyList<DiscoveryResultDto>>
{
    private readonly MusicRecDbContext _db;
    private readonly ICacheService _cache;
    private readonly RecommendationOptions _options;

    public GetDiscoveryQueryHandler(MusicRecDbContext db, ICacheService cache,
        IOptions<RecommendationOptions> options)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<DiscoveryResultDto>> Handle(
        GetDiscoveryQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.Discovery(request.UserId, request.Limit);

        return await _cache.GetOrCreateAsync<IReadOnlyList<DiscoveryResultDto>>(cacheKey, async () =>
        {
        // 加载用户画像
        var profile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        var preferredGenres = ParseFavoriteGenres(profile?.FavoriteGenres)
            .Select(g => g.ToLowerInvariant())
            .ToHashSet();

        // 加载候选曲目
        var candidates = await _db.Set<Track>()
            .Include(t => t.TrackArtists!).ThenInclude(ta => ta.Artist)
            .Include(t => t.TrackGenres!).ThenInclude(tg => tg.Genre)
            .Include(t => t.Album)
            .AsNoTracking()
            .OrderByDescending(t => t.Popularity)
            .Take(_options.CandidatePoolSize)
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return Array.Empty<DiscoveryResultDto>();

        // 70/20/10 探索分配
        var discoveries = DiscoveryEngine.Explore(candidates, request.Limit, preferredGenres);

        return MapToDto(discoveries);
        }, TimeSpan.FromMinutes(10), ct);
    }

    /// <summary>
    /// 从 UserProfile.FavoriteGenres JSON 解析流派列表
    /// </summary>
    private static List<string> ParseFavoriteGenres(string? favoriteGenresJson)
    {
        if (string.IsNullOrEmpty(favoriteGenresJson)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(favoriteGenresJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<DiscoveryResultDto> MapToDto(List<DiscoveryTrack> discoveries)
    {
        return discoveries.Select(d =>
        {
            var track = d.Track;
            var artistsSummary = track.TrackArtists is { Count: > 0 }
                ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist?.Name ?? "未知"))
                : "未知艺术家";

            var genres = track.TrackGenres?
                .Select(tg => tg.Genre?.Name)
                .Where(g => g is not null)
                .Select(g => g!)
                .ToList() ?? new List<string>();

            return new DiscoveryResultDto(
                TrackId: track.Id,
                TrackName: track.Name,
                CoverImageUrl: track.CoverImageUrl,
                DurationMs: track.DurationMs,
                Popularity: track.Popularity,
                ArtistsSummary: artistsSummary,
                AlbumName: track.Album?.Name,
                DiscoveryReason: d.DiscoveryReason,
                Genres: genres
            );
        }).ToList();
    }
}
