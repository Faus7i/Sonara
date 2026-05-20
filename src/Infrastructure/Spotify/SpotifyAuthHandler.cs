using System.Net.Http.Headers;

namespace MusicRec.Spotify;

/// <summary>
/// Spotify 认证委托处理器 — 在每次 HTTP 请求发送前自动注入 Bearer 令牌
/// </summary>
/// <remarks>
/// 替代 SpotifyClient 中每个方法手动调用 SetAuthHeader 的模式：
/// 1. 消除 8 处重复的 SetAuthHeader 调用
/// 2. 线程安全 — 每个请求独立设置 Authorization 头，不修改共享的 HttpClient.DefaultRequestHeaders
/// 3. 令牌由 SpotifyTokenStore 统一管理（含 IMemoryCache 缓存）
/// </remarks>
public class SpotifyAuthHandler : DelegatingHandler
{
    private readonly SpotifyTokenStore _tokenStore;

    public SpotifyAuthHandler(SpotifyTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
