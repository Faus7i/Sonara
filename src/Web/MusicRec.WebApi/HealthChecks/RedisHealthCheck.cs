using Microsoft.Extensions.Diagnostics.HealthChecks;
using MusicRec.Shared.Caching;

namespace MusicRec.WebApi.HealthChecks;

/// <summary>
/// Redis 健康检查 — 验证缓存服务是否正常运行
/// </summary>
/// <remarks>
/// 降级模式（仅内存缓存）时返回 Degraded 而非 Unhealthy，
/// 确保依赖 Redis 的监控不会在开发环境误报。
/// </remarks>
public class RedisHealthCheck : IHealthCheck
{
    private readonly ICacheService _cache;

    public RedisHealthCheck(ICacheService cache) => _cache = cache;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            // 写入 + 读取 + 删除 完整链路测试
            await _cache.SetAsync("health-check", "ok", TimeSpan.FromSeconds(5), ct);
            var value = await _cache.GetAsync<string>("health-check", ct);
            await _cache.RemoveAsync("health-check", ct);

            if (value == "ok")
                return HealthCheckResult.Healthy("缓存服务正常");

            return HealthCheckResult.Degraded("缓存读写不一致（仅内存模式）");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"缓存服务降级：{ex.Message}");
        }
    }
}
