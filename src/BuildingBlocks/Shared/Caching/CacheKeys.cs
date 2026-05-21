namespace MusicRec.Shared.Caching;

/// <summary>
/// 集中式缓存键定义 — 所有缓存键必须通过此类生成，确保命名一致
/// </summary>
public static class CacheKeys
{
    /// <summary>用户个性化推荐缓存</summary>
    public static string Recommendation(Guid userId, int limit) => $"rec:user:{userId}:{limit}";

    /// <summary>相似曲目推荐缓存</summary>
    public static string SimilarTracks(Guid trackId, int limit) => $"rec:similar:{trackId}:{limit}";

    /// <summary>用户探索推荐缓存</summary>
    public static string Discovery(Guid userId, int limit) => $"discovery:user:{userId}:{limit}";

    /// <summary>冷启动推荐缓存（全局共享）</summary>
    public static string ColdStart(int limit) => $"discovery:cold-start:{limit}";

    /// <summary>曲目详情缓存</summary>
    public static string TrackDetail(Guid trackId) => $"catalog:track:{trackId}";

    /// <summary>用户推荐相关缓存前缀（用于批量失效）</summary>
    public static string UserRecommendationPrefix(Guid userId) => $"rec:user:{userId}:";
}
