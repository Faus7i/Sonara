using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace MusicRec.Spotify;

/// <summary>
/// Spotify 基础设施模块 DI 注册
/// </summary>
public static class DependencyInjection
{
    private const int MaxRetryAttempts = 5;
    private const double BackoffBaseSeconds = 2;
    private const int JitterMaxMs = 1000;
    private const int HandlerLifetimeMinutes = 5;

    public static IServiceCollection AddSpotify(this IServiceCollection services)
    {
        // 绑定 Spotify:ClientId / Spotify:ClientSecret 配置
        services.AddOptions<SpotifyOptions>()
            .BindConfiguration(SpotifyOptions.SectionName);

        // Token 缓存和令牌存储
        services.AddMemoryCache();
        services.AddSingleton<SpotifyTokenStore>();

        // 认证委托处理器 — 每次请求前自动注入 Bearer 令牌
        services.AddTransient<SpotifyAuthHandler>();

        // Spotify API 主客户端（BaseAddress = https://api.spotify.com/v1/）
        services.AddHttpClient<ISpotifyClient, SpotifyClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.spotify.com/v1/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddHttpMessageHandler<SpotifyAuthHandler>()
        .AddPolicyHandler(GetRetryPolicy())
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            // 限制连接存活时间，确保 DNS 变更能被及时感知
            PooledConnectionLifetime = TimeSpan.FromMinutes(HandlerLifetimeMinutes)
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(HandlerLifetimeMinutes));

        // Token 端点客户端（BaseAddress = https://accounts.spotify.com）
        services.AddHttpClient("SpotifyAuth", client =>
        {
            client.BaseAddress = new Uri("https://accounts.spotify.com/");
        })
        .AddPolicyHandler(GetRetryPolicy()); // Token 端点也需要重试策略

        return services;
    }

    /// <summary>
    /// 指数退避重试策略 — 处理 5xx、408、429
    /// </summary>
    /// <remarks>
    /// 退避公式：2^retryAttempt 秒 + 0~1000ms 随机抖动（防止惊群效应）。
    /// Spotify 的 Retry-After 头在 onRetry 中记录，但 Polly 的 WaitAndRetryAsync
    /// 在此版本不支持基于响应头动态调整延迟，故使用固定指数退避。
    /// </remarks>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(BackoffBaseSeconds, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, JitterMaxMs))
            );
    }
}
