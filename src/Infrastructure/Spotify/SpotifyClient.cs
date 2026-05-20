using System.Net.Http.Json;
using MusicRec.Spotify.Models;

namespace MusicRec.Spotify;

/// <summary>
/// Spotify Web API 客户端 — 所有 API 调用通过此服务统一发出
/// </summary>
/// <remarks>
/// 认证令牌由 SpotifyAuthHandler（DelegatingHandler）在每次请求前自动注入，
/// 避免多线程竞争 DefaultRequestHeaders。Polly 重试策略在 DI 注册时附加。
/// JSON 反序列化使用 SnakeCaseLower 策略匹配 Spotify 的 snake_case 命名。
/// </remarks>
public class SpotifyClient : ISpotifyClient
{
    private readonly HttpClient _http;

    public SpotifyClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<SearchResponse> SearchAsync(
        string query, string type, int limit = 20, int offset = 0, CancellationToken ct = default)
    {
        var url = $"search?q={Uri.EscapeDataString(query)}&type={type}&limit={limit}&offset={offset}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SearchResponse>(SpotifyJsonDefaults.Options, ct))!;
    }

    public async Task<TrackObject> GetTrackAsync(string spotifyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"tracks/{spotifyId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TrackObject>(SpotifyJsonDefaults.Options, ct))!;
    }

    public async Task<List<TrackObject>> GetTracksAsync(IReadOnlyList<string> spotifyIds, CancellationToken ct = default)
    {
        if (spotifyIds.Count == 0) return new List<TrackObject>();
        var ids = string.Join(",", spotifyIds); // Spotify API 限制每次最多 50 个 ID
        var response = await _http.GetAsync($"tracks?ids={ids}", ct);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<SeveralTracksResponse>(SpotifyJsonDefaults.Options, ct))!;
        return result.Tracks;
    }

    public async Task<ArtistObject> GetArtistAsync(string spotifyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"artists/{spotifyId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ArtistObject>(SpotifyJsonDefaults.Options, ct))!;
    }

    public async Task<AlbumObject> GetAlbumAsync(string spotifyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"albums/{spotifyId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlbumObject>(SpotifyJsonDefaults.Options, ct))!;
    }

    public async Task<AudioFeaturesObject?> GetAudioFeaturesAsync(string spotifyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"audio-features/{spotifyId}", ct);

        // 404 表示 Spotify 暂未分析该曲目（非异常场景）
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AudioFeaturesObject>(SpotifyJsonDefaults.Options, ct);
    }

    public async Task<List<AudioFeaturesObject>> GetAudioFeaturesBatchAsync(
        IReadOnlyList<string> spotifyIds, CancellationToken ct = default)
    {
        if (spotifyIds.Count == 0) return new List<AudioFeaturesObject>();
        var ids = string.Join(",", spotifyIds); // Spotify API 限制每次最多 100 个 ID
        var response = await _http.GetAsync($"audio-features?ids={ids}", ct);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<SeveralAudioFeaturesResponse>(SpotifyJsonDefaults.Options, ct))!;
        // 过滤掉 Spotify 未分析的曲目（API 对未知曲目返回 null 元素）
        return result.AudioFeatures.Where(af => af is not null).ToList();
    }

    public async Task<List<TrackObject>> GetArtistTopTracksAsync(
        string spotifyId, string? market = null, CancellationToken ct = default)
    {
        var url = $"artists/{spotifyId}/top-tracks";
        if (!string.IsNullOrEmpty(market))
            url += $"?market={market}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<ArtistTopTracksResponse>(SpotifyJsonDefaults.Options, ct))!;
        return result.Tracks;
    }
}
