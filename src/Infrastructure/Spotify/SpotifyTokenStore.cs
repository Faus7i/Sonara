using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MusicRec.Spotify.Models;

namespace MusicRec.Spotify;

/// <summary>
/// Spotify Access Token 缓存服务 — 使用 IMemoryCache 缓存令牌，提前 100 秒刷新
/// </summary>
/// <remarks>
/// Client Credentials 令牌有效期 3600 秒，缓存 3500 秒以确保在过期前刷新。
/// 无需线程同步 — IMemoryCache 本身是线程安全的。
/// </remarks>
public class SpotifyTokenStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly SpotifyOptions _options;
    private const string TokenCacheKey = "spotify_access_token";
    // Spotify Client Credentials 令牌有效期 3600 秒。
    // 缓存 3500 秒，提前 100 秒过期留出缓冲，防止在令牌刚好过期时发起 API 调用。
    private const int CacheSeconds = 3500;

    public SpotifyTokenStore(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<SpotifyOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? token) && token is not null)
            return token;

        token = await FetchTokenAsync(ct);
        _cache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(CacheSeconds));
        return token;
    }

    private async Task<string> FetchTokenAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("SpotifyAuth");
        var authBytes = Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync("https://accounts.spotify.com/api/token", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(SpotifyJsonDefaults.Options, ct);
        return result?.AccessToken
            ?? throw new InvalidOperationException("Spotify 令牌响应中缺少 access_token");
    }
}
