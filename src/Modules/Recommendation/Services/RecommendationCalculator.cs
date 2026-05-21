using System.Text.Json;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Catalog.Entities;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.Recommendation.Services;

/// <summary>
/// 推荐算法核心计算器 — 实现混合推荐公式的所有评分函数
/// </summary>
/// <remarks>
/// 推荐公式：
///   FinalScore = 0.35 × AudioSimilarity + 0.25 × BehaviorScore
///              + 0.15 × GenrePreference + 0.10 × Freshness
///              + 0.10 × Diversity + 0.05 × Popularity
///
/// 音频特征缺失时降级：将 0.35 权重重新分配至流派(0.15)、热度(0.10)、新鲜度(0.10)
/// 所有方法为静态纯函数，不依赖外部状态，便于单元测试。
/// </remarks>
public static class RecommendationCalculator
{
    private const float TempoNormalizer = 200f;

    /// <summary>
    /// 余弦相似度 — 衡量向量方向的相似程度（对长度不敏感）
    /// </summary>
    /// <remarks>
    /// 原始值域 [-1, 1]（-1=完全相反，0=正交无关，1=完全相同方向）。
    /// 归一化到 [0, 1] 便于与其他评分组件（均已归一化）直接加权求和。
    ///
    /// 与 EuclideanSimilarity 的选择：
    ///   - 余弦：对 Tempo/200 归一化误差不敏感（只关心比例关系）
    ///   - 欧氏：对绝对差异更敏感（向量 A 和 B 的"距离"），适合需要精确匹配的场景
    ///   当前主推荐流程使用余弦相似度（默认），欧氏距离作为备选方案提供。
    /// </remarks>
    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;

        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        if (denom < float.Epsilon) return 0f;

        // 归一化到 [0, 1]（原始余弦值域 [-1, 1]）
        return (dot / denom + 1f) / 2f;
    }

    /// <summary>
    /// 欧氏距离相似度 — 衡量向量在多维空间中的绝对距离
    /// </summary>
    /// <remarks>
    /// 公式：1 / (1 + √(Σ(aᵢ - bᵢ)²))，值域 (0, 1]
    ///   - 距离为 0（向量完全相同）→ 相似度 1.0
    ///   - 距离趋近 ∞ → 相似度趋近 0
    ///
    /// 与余弦相似度的区别：欧氏距离对每个维度的绝对值敏感，
    /// 适合需要"用户画像向量与曲目向量在数值上接近"的场景。
    /// 余弦相似度则只关心方向（比例），适合"偏好模式相似"的判断。
    /// </remarks>
    public static float EuclideanSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;

        float sumSquares = 0;
        for (int i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            sumSquares += diff * diff;
        }

        var distance = MathF.Sqrt(sumSquares);
        return 1f / (1f + distance);
    }

    /// <summary>
    /// 音频特征相似度 — 用户画像向量 vs 候选曲目特征向量的余弦相似度
    /// </summary>
    public static double ComputeAudioSimilarity(float[] userVector, TrackAudioFeature af)
    {
        var trackVector = af.ToVector();
        return CosineSimilarity(userVector, trackVector);
    }

    /// <summary>
    /// 行为评分 — 基于用户是否喜欢该曲目的艺术家
    /// </summary>
    /// <param name="track">候选曲目（需 Include TrackArtists.Artist）</param>
    /// <param name="likedArtistIds">用户收藏过的艺术家 ID 集合</param>
    /// <param name="playedTrackIds">用户已播放过的曲目 ID 集合</param>
    /// <param name="explorationLevel">用户探索意愿 0~1</param>
    public static double ComputeBehaviorScore(Track track, HashSet<Guid> likedArtistIds,
        HashSet<Guid> playedTrackIds, double explorationLevel)
    {
        // 已播放过 → 极低分（用户已听过）
        if (playedTrackIds.Contains(track.Id)) return 0.05;

        var trackArtistIds = track.TrackArtists?.Select(ta => ta.ArtistId).ToHashSet() ?? new HashSet<Guid>();
        var hasLikedArtist = trackArtistIds.Overlaps(likedArtistIds);

        // 探索意愿越高，基础行为分越低（用户愿意尝试新艺术家）
        var explorationBoost = explorationLevel * 0.2;

        return hasLikedArtist ? 0.6 + explorationBoost : 0.3 + explorationBoost;
    }

    /// <summary>
    /// 流派偏好评分 — 曲目流派与用户偏好流派的匹配度
    /// </summary>
    public static double ComputeGenrePreferenceScore(Track track, List<string> favoriteGenres)
    {
        if (favoriteGenres.Count == 0) return 0.5; // 新用户无偏好 → 中性分

        var trackGenreNames = track.TrackGenres?
            .Select(tg => tg.Genre?.Name?.ToLowerInvariant())
            .Where(g => g is not null)
            .Select(g => g!)
            .ToHashSet() ?? new HashSet<string>();

        if (trackGenreNames.Count == 0) return 0.5;

        var matches = favoriteGenres
            .Select(g => g.ToLowerInvariant())
            .Count(g => trackGenreNames.Contains(g));

        return Math.Min(1.0, (double)matches / Math.Max(1, trackGenreNames.Count));
    }

    /// <summary>
    /// 新鲜度评分 — 基于发行日期，线性衰减（10 年内从 1.0 衰减到 0.0）
    /// </summary>
    public static double ComputeFreshnessScore(string? releaseDate)
    {
        if (string.IsNullOrEmpty(releaseDate)) return 0.5;
        if (!DateTime.TryParse(releaseDate, out var date)) return 0.5;

        var daysSinceRelease = (DateTime.UtcNow - date).TotalDays;
        return Math.Max(0, 1.0 - daysSinceRelease / 3650.0);
    }

    /// <summary>
    /// 热度评分 — 将 Spotify popularity (0-100) 归一化到 [0, 1]
    /// </summary>
    public static double ComputePopularityScore(int popularity)
    {
        return popularity / 100.0;
    }

    /// <summary>
    /// 从 UserProfile 构建 5 维用户偏好特征向量
    /// 维度： [Energy, Danceability, Valence, Tempo/200, Acousticness]
    /// </summary>
    public static float[] BuildUserFeatureVector(UserProfile profile)
    {
        return new[]
        {
            (float)profile.AvgEnergy,
            (float)profile.AvgDanceability,
            (float)profile.AvgValence,
            (float)(profile.AvgTempo / TempoNormalizer),
            (float)profile.AvgAcousticness
        };
    }

    /// <summary>
    /// 解析 UserProfile.FavoriteGenres JSON 数组为字符串列表
    /// </summary>
    public static List<string> ParseFavoriteGenres(string? favoriteGenresJson)
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

    /// <summary>
    /// 生成推荐原因（中文）— 基于最高贡献的评分维度
    /// </summary>
    /// <param name="track">候选曲目</param>
    /// <param name="af">音频特征（可为 null 表示降级模式）</param>
    /// <param name="favoriteGenres">用户偏好流派列表（已从 JSON 解析）</param>
    /// <param name="likedArtistIds">用户收藏过的艺术家 ID 集合</param>
    /// <param name="userVector">用户 5 维特征向量</param>
    /// <remarks>
    /// 原因按优先级判定：音频匹配 > 艺术家喜好 > 流派偏好 > 热门 > 综合
    /// 每级判定失败后自动降级到下一优先级，确保总能返回一个有意义的理由。
    /// </remarks>
    public static string DetermineReason(Track track, TrackAudioFeature? af,
        List<string> favoriteGenres, HashSet<Guid> likedArtistIds, float[] userVector)
    {
        var trackArtistIds = track.TrackArtists?.Select(ta => ta.ArtistId).ToHashSet() ?? new HashSet<Guid>();

        // 优先级 1：音频特征高度匹配（相似度 > 0.85，即余弦归一化后非常接近）
        if (af is not null && userVector is { Length: 5 })
        {
            var similarity = CosineSimilarity(userVector, af.ToVector());
            if (similarity > 0.85) return "音频特征高度匹配";
        }

        // 优先级 2：用户收藏过该曲目的艺术家（最直观的行为信号）
        if (trackArtistIds.Overlaps(likedArtistIds))
        {
            var likedArtist = track.TrackArtists!
                .First(ta => likedArtistIds.Contains(ta.ArtistId)).Artist?.Name;
            if (!string.IsNullOrEmpty(likedArtist))
                return $"因为你喜欢 {likedArtist}";
        }

        // 优先级 3：曲目流派与用户偏好流派匹配
        var trackGenres = track.TrackGenres?
            .Select(tg => tg.Genre?.Name)
            .Where(g => g is not null)
            .Select(g => g!)
            .ToList() ?? new List<string>();

        var matchedGenre = trackGenres.FirstOrDefault(g =>
            favoriteGenres.Any(fg => fg.Equals(g, StringComparison.OrdinalIgnoreCase)));

        if (matchedGenre is not null)
            return $"基于你喜欢的风格 · {matchedGenre}";

        // 优先级 4：热门曲目（流行度 > 70）
        if (track.Popularity > 70) return "热门曲目推荐";

        // 优先级 5：兜底综合推荐
        return "综合评分推荐";
    }
}
