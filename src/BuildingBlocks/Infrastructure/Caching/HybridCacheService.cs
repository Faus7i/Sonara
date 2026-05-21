using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicRec.Shared.Caching;
using StackExchange.Redis;

namespace MusicRec.Infrastructure.Caching;

/// <summary>
/// 双级混合缓存服务 — L1 内存缓存 + L2 Redis 分布式缓存
/// </summary>
/// <remarks>
/// Redis 不可用时自动降级为纯内存模式，确保开发环境和生产故障时系统正常运行。
/// 使用 ConcurrentDictionary 管理 L1 缓存键集合，支持前缀匹配删除。
/// </remarks>
public sealed class HybridCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConnectionMultiplexer? _redis;
    private readonly IDatabase? _redisDb;
    private readonly CacheServiceOptions _options;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _memoryKeys = new();
    private readonly JsonSerializerOptions _jsonOptions;

    // 防止同一 Key 的缓存击穿（惊群效应）
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public HybridCacheService(
        IMemoryCache memoryCache,
        IOptions<CacheServiceOptions> options,
        ILogger<HybridCacheService> logger)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        if (!string.IsNullOrEmpty(_options.RedisConnectionString))
        {
            try
            {
                _redis = ConnectionMultiplexer.Connect(_options.RedisConnectionString);
                _redisDb = _redis.GetDatabase();
                _logger.LogInformation("Redis 连接成功");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 连接失败，降级为纯内存缓存模式");
            }
        }
        else
        {
            _logger.LogInformation("未配置 Redis 连接字符串，使用纯内存缓存模式");
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // L1：内存缓存
        if (_memoryCache.TryGetValue(key, out var cached))
        {
            if (cached is T typedValue)
                return typedValue;

            // 处理 JsonElement（从 Redis 反序列化回来的原始值）
            if (cached is JsonElement jsonElement)
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions);
        }

        // L2：Redis
        if (_redisDb is not null)
        {
            try
            {
                var redisValue = await _redisDb.StringGetAsync(key);
                if (redisValue.HasValue)
                {
                    var value = JsonSerializer.Deserialize<T>(redisValue!, _jsonOptions);
                    if (value is not null)
                    {
                        // 回填 L1
                        var expiry = GetTtl(key);
                        _memoryCache.Set(key, value, expiry);
                        _memoryKeys.TryAdd(key, 0);
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 读取失败，Key={Key}", key);
            }
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var ttl = expiry ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds);

        // L1：内存缓存
        _memoryCache.Set(key, value, ttl);
        _memoryKeys.TryAdd(key, 0);

        // L2：Redis
        if (_redisDb is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                await _redisDb.StringSetAsync(key, json, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 写入失败，Key={Key}", key);
            }
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _memoryCache.Remove(key);
        _memoryKeys.TryRemove(key, out _);

        if (_redisDb is not null)
        {
            try
            {
                _redisDb.KeyDeleteAsync(key, CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 删除失败，Key={Key}", key);
            }
        }

        return Task.CompletedTask;
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // L1：遍历内存键集合，匹配前缀删除
        var matchingKeys = _memoryKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in matchingKeys)
        {
            _memoryCache.Remove(key);
            _memoryKeys.TryRemove(key, out _);
        }

        // L2：Redis SCAN 遍历（不阻塞，使用 FireAndForget）
        if (_redisDb is not null)
        {
            try
            {
                var server = _redis!.GetServer(_redis.GetEndPoints()[0]);
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                if (keys.Length > 0)
                {
                    await _redisDb.KeyDeleteAsync(keys, CommandFlags.FireAndForget);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 前缀删除失败，Prefix={Prefix}", prefix);
            }
        }

        _logger.LogDebug("缓存前缀失效完成，Prefix={Prefix}，L1清理={L1Count}条",
            prefix, matchingKeys.Count);
    }

    /// <summary>
    /// 根据缓存键推断 TTL（用于 Redis 回填 L1 时保持一致的过期时间）
    /// </summary>
    private TimeSpan GetTtl(string key)
    {
        if (key.StartsWith("rec:user:")) return TimeSpan.FromSeconds(_options.RecommendationTtlSeconds);
        if (key.StartsWith("rec:similar:")) return TimeSpan.FromSeconds(_options.SimilarTracksTtlSeconds);
        if (key.StartsWith("discovery:user:")) return TimeSpan.FromSeconds(_options.DiscoveryTtlSeconds);
        if (key.StartsWith("discovery:cold-start:")) return TimeSpan.FromSeconds(_options.ColdStartTtlSeconds);
        if (key.StartsWith("catalog:")) return TimeSpan.FromSeconds(_options.CatalogTtlSeconds);
        return TimeSpan.FromSeconds(_options.DefaultTtlSeconds);
    }

    public void Dispose()
    {
        _redis?.Dispose();
        _locks.Values.ToList().ForEach(l => l.Dispose());
        _locks.Clear();
    }
}
