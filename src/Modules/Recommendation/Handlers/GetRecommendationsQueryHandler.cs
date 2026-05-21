using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Catalog.Entities;
using MusicRec.Favorites.Entities;
using MusicRec.Infrastructure;
using MusicRec.Recommendation.DTOs;
using MusicRec.Recommendation.Queries;
using MusicRec.Recommendation.Services;
using MusicRec.Shared;
using MusicRec.Shared.Caching;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.Recommendation.Handlers;

/// <summary>
/// 个性化推荐查询处理器 — 实现混合推荐算法的完整流程
/// </summary>
/// <remarks>
/// 算法流程（8 阶段）：
///   1. 加载用户画像（UserProfile，含 5 维平均音频特征向量）
///   2. 加载行为数据（已播放曲目 ID、已收藏艺术家 ID）
///   3. 加载候选曲目（所有未被用户播放过的曲目，含流派和艺术家导航属性）
///   4. 批量加载候选曲目的音频特征（有则走完整公式，无则降级）
///   5. 逐曲目评分（完整公式 / 降级公式）
///   6. 按分数降序排序
///   7. 探索分配：70% 高分 + 20% 相邻流派 + 10% 随机探索
///   8. 多样性去重（每位艺术家最多 2 首），映射 DTO
///
/// 候选池大小：为控制内存与计算量，默认取 200 首未播放曲目。
/// 在毕业设计数据量（~1000 首）下完全可行。
/// </remarks>
public class GetRecommendationsQueryHandler : IRequestHandler<GetRecommendationsQuery, IReadOnlyList<RecommendationResultDto>>
{
    private readonly MusicRecDbContext _db;
    private readonly ICacheService _cache;
    private readonly RecommendationOptions _options;

    public GetRecommendationsQueryHandler(MusicRecDbContext db, ICacheService cache,
        IOptions<RecommendationOptions> options)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<RecommendationResultDto>> Handle(
        GetRecommendationsQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.Recommendation(request.UserId, request.Limit);

        return await _cache.GetOrCreateAsync<IReadOnlyList<RecommendationResultDto>>(cacheKey, async () =>
        {
        // ── 阶段 1：加载用户画像 ─────────────────────
        var profile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        var userVector = profile is not null
            ? RecommendationCalculator.BuildUserFeatureVector(profile)
            : new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f }; // 默认中性向量

        var favoriteGenres = RecommendationCalculator.ParseFavoriteGenres(profile?.FavoriteGenres);
        var explorationLevel = profile?.ExplorationLevel ?? 0.3;

        // ── 阶段 2：加载行为数据 ─────────────────────
        var playedTrackIds = await _db.Set<UserPlayHistory>()
            .Where(h => h.UserId == request.UserId)
            .Select(h => h.TrackId)
            .Distinct()
            .ToListAsync(ct);

        var playedSet = new HashSet<Guid>(playedTrackIds);

        var likedArtistIds = await _db.Set<UserLike>()
            .Where(l => l.UserId == request.UserId)
            .Join(_db.Set<Track>(), l => l.TrackId, t => t.Id, (l, t) => t)
            .SelectMany(t => t.TrackArtists!.Select(ta => ta.ArtistId))
            .Distinct()
            .ToListAsync(ct);

        var likedArtistSet = new HashSet<Guid>(likedArtistIds);

        // ── 阶段 3：加载候选曲目（逐步构建 IQueryable，避免重复 Include 链）─
        var candidateQuery = _db.Set<Track>()
            .Include(t => t.TrackArtists!).ThenInclude(ta => ta.Artist)
            .Include(t => t.TrackGenres!).ThenInclude(tg => tg.Genre)
            .Include(t => t.Album)
            .AsNoTracking()
            .AsQueryable();

        // 排除已播放曲目（新用户 → 全量候选）
        if (playedSet.Count > 0)
            candidateQuery = candidateQuery.Where(t => !playedSet.Contains(t.Id));

        var candidates = await candidateQuery
            .OrderByDescending(t => t.Popularity)
            .Take(_options.CandidatePoolSize)
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return Array.Empty<RecommendationResultDto>();

        // ── 阶段 4：批量加载音频特征 ─────────────────
        var candidateSpotifyIds = candidates.Select(t => t.SpotifyTrackId).ToList();
        var audioFeatureList = await _db.Set<TrackAudioFeature>()
            .Where(af => candidateSpotifyIds.Contains(af.SpotifyTrackId))
            .ToListAsync(ct);

        var audioBySpotifyId = audioFeatureList.ToDictionary(af => af.SpotifyTrackId);

        // ── 阶段 5：逐曲目评分 ───────────────────────
        var scored = new List<ScoredTrack>();
        foreach (var track in candidates)
        {
            double score;
            string reason;

            if (audioBySpotifyId.TryGetValue(track.SpotifyTrackId, out var af))
            {
                // 完整公式：有音频特征
                var audioSim = RecommendationCalculator.ComputeAudioSimilarity(userVector, af);
                var behavior = RecommendationCalculator.ComputeBehaviorScore(
                    track, likedArtistSet, playedSet, explorationLevel);
                var genrePref = RecommendationCalculator.ComputeGenrePreferenceScore(track, favoriteGenres);
                var freshness = RecommendationCalculator.ComputeFreshnessScore(track.ReleaseDate);
                var popularity = RecommendationCalculator.ComputePopularityScore(track.Popularity);

                score = 0.35 * audioSim + 0.25 * behavior + 0.15 * genrePref
                      + 0.10 * freshness + 0.10 * 0.5 + 0.05 * popularity;

                reason = RecommendationCalculator.DetermineReason(
                    track, af, favoriteGenres, likedArtistSet, userVector);
            }
            else
            {
                // 降级公式：无音频特征 → 权重重新分配
                var genrePref = RecommendationCalculator.ComputeGenrePreferenceScore(track, favoriteGenres);
                var behavior = RecommendationCalculator.ComputeBehaviorScore(
                    track, likedArtistSet, playedSet, explorationLevel);
                var freshness = RecommendationCalculator.ComputeFreshnessScore(track.ReleaseDate);
                var popularity = RecommendationCalculator.ComputePopularityScore(track.Popularity);

                score = 0.30 * genrePref + 0.25 * behavior + 0.15 * popularity
                      + 0.20 * freshness + 0.10 * 0.5;

                reason = $"风格匹配 · {track.TrackGenres?.FirstOrDefault()?.Genre?.Name ?? "综合推荐"}";
            }

            scored.Add(new ScoredTrack(track, score, reason));
        }

        // ── 阶段 6：按分数降序排序 ───────────────────
        scored.Sort((a, b) => b.Score.CompareTo(a.Score));

        // ── 阶段 7：探索分配（70/20/10）───────────────
        var finalSelection = ApplyExploration(scored, favoriteGenres, request.Limit);

        // ── 阶段 8：多样性去重 + 映射 DTO ──────────────
        return ApplyDiversityAndMap(finalSelection, request.Limit);
        }, TimeSpan.FromMinutes(3), ct);
    }

    /// <summary>
    /// 探索分配：70% 高分 + 20% 相邻流派 + 10% 随机探索
    /// </summary>
    /// <remarks>
    /// 多样性不直接体现在逐曲目评分中（FinalScore 的 Diversity 项使用 0.5 占位），
    /// 而是通过此方法的 70/20/10 分配 + ApplyDiversityAndMap 的艺术家去重来实现。
    /// 这样设计的原因：Diversity 本质上是推荐列表的整体属性（集合内的差异度），
    /// 而非单首曲目的固有属性，因此更适合在排序后通过采样策略注入。
    /// </remarks>
    private List<ScoredTrack> ApplyExploration(
        List<ScoredTrack> scored, List<string> favoriteGenres, int limit)
    {
        var exploitCount = (int)(limit * _options.ExploitRatio);
        var adjacentCount = (int)(limit * _options.AdjacentRatio);
        var novelCount = limit - exploitCount - adjacentCount;

        var result = new List<ScoredTrack>();

        // 70%：最高分曲目
        result.AddRange(scored.Take(exploitCount));

        // 20%：相邻流派曲目（与用户偏好流派有交集）
        var favoriteSet = favoriteGenres.Select(g => g.ToLowerInvariant()).ToHashSet();
        var adjacent = scored
            .Skip(exploitCount)
            .Where(s => s.Track.TrackGenres?.Any(tg =>
                tg.Genre?.Name is not null && favoriteSet.Contains(tg.Genre.Name.ToLowerInvariant())) == true)
            .Take(adjacentCount)
            .Select(s => s with { Reason = "发现类似风格 · " + s.Reason });
        result.AddRange(adjacent);

        // 10%：新风格探索（不在偏好流派内的曲目，随机采样）
        var novel = scored
            .Skip(exploitCount)
            .Where(s => s.Track.TrackGenres?.All(tg =>
                tg.Genre?.Name is null || !favoriteSet.Contains(tg.Genre.Name.ToLowerInvariant())) != false)
            .OrderBy(_ => Random.Shared.Next())
            .Take(novelCount)
            .Select(s => s with { Reason = "探索新风格 · 尝试不同音乐类型" });
        result.AddRange(novel);

        // 不足时用后续曲目填充
        var remaining = scored.Skip(result.Count).Take(limit - result.Count);
        result.AddRange(remaining);

        return result;
    }

    /// <summary>
    /// 多样性去重：每位艺术家最多 2 首曲目，然后映射 DTO
    /// </summary>
    private static List<RecommendationResultDto> ApplyDiversityAndMap(
        List<ScoredTrack> scored, int limit)
    {
        var artistCounts = new Dictionary<Guid, int>();
        var result = new List<RecommendationResultDto>();

        foreach (var (track, score, reason) in scored)
        {
            if (result.Count >= limit) break;

            var primaryArtistId = track.TrackArtists?.FirstOrDefault()?.ArtistId;
            if (primaryArtistId.HasValue)
            {
                artistCounts.TryGetValue(primaryArtistId.Value, out var count);
                if (count >= 2) continue;
                artistCounts[primaryArtistId.Value] = count + 1;
            }

            var artistsSummary = track.TrackArtists is { Count: > 0 }
                ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist?.Name ?? "未知"))
                : "未知艺术家";

            var genres = track.TrackGenres?
                .Select(tg => tg.Genre?.Name)
                .Where(g => g is not null)
                .Select(g => g!)
                .ToList() ?? new List<string>();

            result.Add(new RecommendationResultDto(
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
            ));
        }

        return result;
    }

    /// <summary>
    /// 内部评分记录 — 用于排序和探索分配
    /// </summary>
    private record ScoredTrack(Track Track, double Score, string Reason);
}
