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

        // Spotify 开发模式 App 可能对某些参数组合返回 400
        // 不抛异常，返回空结果让前端正常展示而非白屏
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            var truncated = errorBody.Length > 200 ? errorBody[..200] : errorBody;
            throw new InvalidOperationException(
                $"Spotify 搜索失败 (HTTP {(int)response.StatusCode}): {truncated}");
        }

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

    // ─── 播放控制 API ────────────────────────────────────────
    // 注意：这些端点需要 user-modify-playback-state / user-read-playback-state scope，
    // 当前 Client Credentials OAuth 流程仅获取了基础数据读取权限。
    // Phase 3 先搭建 API 骨架，后续通过 Authorization Code + PKCE 扩展 scope 即可激活。
    //
    // Spotify Web API 播放端点映射：
    //   GET  me/player              → 获取播放状态
    //   PUT  me/player/play         → 开始/恢复播放
    //   PUT  me/player/pause        → 暂停
    //   POST me/player/next         → 下一首
    //   POST me/player/previous     → 上一首
    //   PUT  me/player/volume       → 音量
    //   PUT  me/player/seek         → 跳转
    //   PUT  me/player/repeat       → 重复模式
    //   PUT  me/player/shuffle      → 随机播放
    //   GET  me/player/devices      → 设备列表
    //   PUT  me/player              → 转移播放

    public async Task<PlaybackStateObject?> GetPlaybackStateAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("me/player", ct);
        // 204: 无活跃播放设备; 404: 无活跃播放会话（Client Credentials 无用户 scope）
        if (response.StatusCode is System.Net.HttpStatusCode.NoContent or System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlaybackStateObject>(SpotifyJsonDefaults.Options, ct);
    }

    public async Task<List<DeviceObject>> GetAvailableDevicesAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("me/player/devices", ct);
        // 404: 无活跃设备或当前认证模式无用户 scope
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new List<DeviceObject>();
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<DevicesResponse>(SpotifyJsonDefaults.Options, ct))!;
        return result.Devices;
    }

    public async Task StartPlaybackAsync(string? deviceId = null, IReadOnlyList<string>? uris = null,
        string? contextUri = null, int? positionMs = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl("me/player/play", deviceId);

        // 仅在有内容时传 body
        object? body = null;
        if (uris is { Count: > 0 } || contextUri is not null || positionMs is not null)
        {
            body = new
            {
                uris,
                context_uri = contextUri,
                offset = positionMs.HasValue ? new { position = 0 } : null,
                position_ms = positionMs
            };
        }

        var response = await SendJsonAsync(HttpMethod.Put, url, body, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task PausePlaybackAsync(string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl("me/player/pause", deviceId);
        var response = await _http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SkipToNextAsync(string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl("me/player/next", deviceId);
        var response = await _http.PostAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SkipToPreviousAsync(string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl("me/player/previous", deviceId);
        var response = await _http.PostAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetVolumeAsync(int volumePercent, string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl($"me/player/volume?volume_percent={volumePercent}", deviceId);
        var response = await _http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SeekToPositionAsync(int positionMs, string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl($"me/player/seek?position_ms={positionMs}", deviceId);
        var response = await _http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetRepeatModeAsync(string state, string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl($"me/player/repeat?state={state}", deviceId);
        var response = await _http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetShuffleAsync(bool state, string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildPlayerUrl($"me/player/shuffle?state={state.ToString().ToLowerInvariant()}", deviceId);
        var response = await _http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task TransferPlaybackAsync(string deviceId, bool play = false, CancellationToken ct = default)
    {
        var body = new { device_ids = new[] { deviceId }, play };
        var response = await SendJsonAsync(HttpMethod.Put, "me/player", body, ct);
        response.EnsureSuccessStatusCode();
    }

    // ─── 播放控制辅助方法 ──────────────────────────────

    /// <summary>为播放 API URL 追加可选的 device_id 查询参数</summary>
    private static string BuildPlayerUrl(string baseUrl, string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return baseUrl;
        var separator = baseUrl.Contains('?') ? '&' : '?';
        return $"{baseUrl}{separator}device_id={Uri.EscapeDataString(deviceId)}";
    }

    /// <summary>发送带 JSON Body 的 HTTP 请求</summary>
    private async Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: SpotifyJsonDefaults.Options);
        return await _http.SendAsync(request, ct);
    }
}
