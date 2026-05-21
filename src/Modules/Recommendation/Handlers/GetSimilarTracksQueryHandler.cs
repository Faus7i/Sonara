using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Catalog.Entities;
using MusicRec.Infrastructure;
using MusicRec.Recommendation.DTOs;
using MusicRec.Recommendation.Queries;
using MusicRec.Recommendation.Services;
using MusicRec.Shared;
using MusicRec.Shared.Caching;

namespace MusicRec.Recommendation.Handlers;

/// <summary>
/// 相似曲目查询处理器 — 基于音频特征余弦相似度 + 流派/艺术家重叠度
/// </summary>
/// <remarks>
/// 评分权重：
///   有音频特征：0.6 × 音频相似度 + 0.2 × 流派重叠 + 0.2 × 同艺术家
///   无音频特征：0.5 × 流派重叠 + 0.3 × 同艺术家 + 0.2 × 热度
///
/// 源曲目自身被排除，候选池限制 200 首以控制计算量。
/// </remarks>
public class GetSimilarTracksQueryHandler : IRequestHandler<GetSimilarTracksQuery, IReadOnlyList<RecommendationResultDto>>
{
    private readonly MusicRecDbContext _db;
    private readonly ICacheService _cache;
    private readonly RecommendationOptions _options;

    public GetSimilarTracksQueryHandler(MusicRecDbContext db, ICacheService cache,
        IOptions<RecommendationOptions> options)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<RecommendationResultDto>> Handle(
        GetSimilarTracksQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.SimilarTracks(request.TrackId, request.Limit);

        return await _cache.GetOrCreateAsync<IReadOnlyList<RecommendationResultDto>>(cacheKey, async () =>
        {
        // ── 阶段 1：加载源曲目 ────────────────────────
        var source = await _db.Set<Track>()
            .Include(t => t.TrackArtists!).ThenInclude(ta => ta.Artist)
            .Include(t => t.TrackGenres!).ThenInclude(tg => tg.Genre)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TrackId, ct);

        if (source is null)
            throw new NotFoundException($"曲目 {request.TrackId} 不存在");

        var sourceArtistIds = source.TrackArtists?
            .Select(ta => ta.ArtistId).ToHashSet() ?? new HashSet<Guid>();

        var sourceGenreNames = source.TrackGenres?
            .Select(tg => tg.Genre?.Name?.ToLowerInvariant())
            .Where(g => g is not null)
            .Select(g => g!)
            .ToHashSet() ?? new HashSet<string>();

        // ── 阶段 2：加载源曲目音频特征 ─────────────────
        var sourceAf = await _db.Set<TrackAudioFeature>()
            .FirstOrDefaultAsync(af => af.SpotifyTrackId == source.SpotifyTrackId, ct);

        // ── 阶段 3：加载候选曲目（排除自身）───────────
        var candidates = await _db.Set<Track>()
            .Include(t => t.TrackArtists!).ThenInclude(ta => ta.Artist)
            .Include(t => t.TrackGenres!).ThenInclude(tg => tg.Genre)
            .Include(t => t.Album)
            .Where(t => t.Id != request.TrackId)
            .AsNoTracking()
            .OrderByDescending(t => t.Popularity)
            .Take(_options.CandidatePoolSize)
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return Array.Empty<RecommendationResultDto>();

        // ── 阶段 4：批量加载候选音频特征 ──────────────
        var candidateSpotifyIds = candidates.Select(t => t.SpotifyTrackId).ToList();
        var afList = await _db.Set<TrackAudioFeature>()
            .Where(af => candidateSpotifyIds.Contains(af.SpotifyTrackId))
            .ToListAsync(ct);
        var afBySpotifyId = afList.ToDictionary(af => af.SpotifyTrackId);

        // ── 阶段 5：逐曲目评分 ────────────────────────
        var scored = new List<(Track Track, double Score, string Reason)>();
        foreach (var candidate in candidates)
        {
            double score;
            var candidateArtistIds = candidate.TrackArtists?
                .Select(ta => ta.ArtistId).ToHashSet() ?? new HashSet<Guid>();
            var candidateGenreNames = candidate.TrackGenres?
                .Select(tg => tg.Genre?.Name?.ToLowerInvariant())
                .Where(g => g is not null)
                .Select(g => g!)
                .ToHashSet() ?? new HashSet<string>();

            // 流派重叠度使用 Jaccard 相似度（交集/并集），比简单匹配比例更公平
            // 例如：源有 2 个流派、候选有 5 个流派、共同 1 个 → 1/6 ≈ 0.17
            var genreOverlap = sourceGenreNames.Count > 0 && candidateGenreNames.Count > 0
                ? (double)sourceGenreNames.Intersect(candidateGenreNames).Count()
                  / Math.Max(1, sourceGenreNames.Union(candidateGenreNames).Count())
                : 0.5;

            var sameArtist = sourceArtistIds.Overlaps(candidateArtistIds) ? 1.0 : 0.0;
            var popularity = candidate.Popularity / 100.0;

            if (sourceAf is not null && afBySpotifyId.TryGetValue(candidate.SpotifyTrackId, out var candidateAf))
            {
                var audioSim = RecommendationCalculator.CosineSimilarity(
                    sourceAf.ToVector(), candidateAf.ToVector());
                score = 0.6 * audioSim + 0.2 * genreOverlap + 0.2 * sameArtist;
            }
            else
            {
                score = 0.5 * genreOverlap + 0.3 * sameArtist + 0.2 * popularity;
            }

            var reason = sameArtist > 0.9
                ? $"同艺术家 · {candidate.TrackArtists?.FirstOrDefault()?.Artist?.Name ?? "未知"}"
                : genreOverlap > 0.6
                    ? $"同风格 · {candidateGenreNames.FirstOrDefault()}"
                    : $"与 {source.Name} 相似";

            scored.Add((candidate, score, reason));
        }

        // ── 阶段 6：降序排序 + 取 Top N ────────────────
        scored.Sort((a, b) => b.Score.CompareTo(a.Score));

        // ── 阶段 7：映射 DTO ──────────────────────────
        return scored.Take(request.Limit).Select(s =>
        {
            var (track, score, reason) = s;
            var artistsSummary = track.TrackArtists is { Count: > 0 }
                ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist?.Name ?? "未知"))
                : "未知艺术家";

            var genres = track.TrackGenres?
                .Select(tg => tg.Genre?.Name)
                .Where(g => g is not null)
                .Select(g => g!)
                .ToList() ?? new List<string>();

            return new RecommendationResultDto(
                TrackId: track.Id,
                TrackName: track.Name,
                CoverImageUrl: track.CoverImageUrl,
                DurationMs: track.DurationMs,
                Popularity: track.Popularity,
                ArtistsSummary: artistsSummary,
                AlbumName: track.Album?.Name,
                Score: Math.Round(score, 4),
                Reason: reason,
                Genres: genres
            );
        }).ToList();
        }, TimeSpan.FromMinutes(30), ct);
    }
}
