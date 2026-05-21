namespace MusicRec.Shared.Caching;

/// <summary>
/// 缓存服务抽象 — 支持 Redis 分布式缓存 + 内存缓存自动降级
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 从缓存获取值，未命中返回 default
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// 写入缓存，expiry 为 null 时使用默认 TTL
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// 删除单个缓存键
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 按前缀批量删除缓存键（用于用户级缓存失效）
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

/// <summary>
/// ICacheService 扩展方法
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Cache-Aside 模式：缓存命中直接返回，未命中调用 factory 并自动写入缓存
    /// </summary>
    public static async Task<T> GetOrCreateAsync<T>(
        this ICacheService cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<T>(key, ct);
        if (cached is not null)
            return cached;

        var value = await factory();
        await cache.SetAsync(key, value, expiry, ct);
        return value;
    }
}
