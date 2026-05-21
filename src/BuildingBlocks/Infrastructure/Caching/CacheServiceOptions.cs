namespace MusicRec.Infrastructure.Caching;

/// <summary>
/// 缓存配置选项 — 绑定自 appsettings.json 的 "Caching" 节
/// </summary>
public class CacheServiceOptions
{
    public const string SectionName = "Caching";

    /// <summary>Redis 连接字符串，null 或空则仅使用内存缓存</summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>默认缓存过期时间（秒）</summary>
    public int DefaultTtlSeconds { get; set; } = 300;

    /// <summary>推荐缓存 TTL（秒），默认 3 分钟</summary>
    public int RecommendationTtlSeconds { get; set; } = 180;

    /// <summary>相似曲目缓存 TTL（秒），默认 30 分钟</summary>
    public int SimilarTracksTtlSeconds { get; set; } = 1800;

    /// <summary>探索推荐缓存 TTL（秒），默认 10 分钟</summary>
    public int DiscoveryTtlSeconds { get; set; } = 600;

    /// <summary>冷启动缓存 TTL（秒），默认 1 小时</summary>
    public int ColdStartTtlSeconds { get; set; } = 3600;

    /// <summary>曲目/目录数据缓存 TTL（秒），默认 1 小时</summary>
    public int CatalogTtlSeconds { get; set; } = 3600;
}
