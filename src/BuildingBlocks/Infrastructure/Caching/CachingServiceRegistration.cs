using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Shared.Caching;

namespace MusicRec.Infrastructure.Caching;

/// <summary>
/// 缓存服务 DI 注册 — 在 Program.cs 中调用 AddCaching()
/// </summary>
public static class CachingServiceRegistration
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheServiceOptions>(
            configuration.GetSection(CacheServiceOptions.SectionName));

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, HybridCacheService>();

        return services;
    }
}
