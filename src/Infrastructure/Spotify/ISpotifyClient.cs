using MusicRec.Spotify.Models;

namespace MusicRec.Spotify;

/// <summary>
/// Spotify Web API 客户端接口 — 封装所有 Spotify API 调用
/// </summary>
public interface ISpotifyClient
{
    /// <summary>搜索曲目/艺术家/专辑</summary>
    Task<SearchResponse> SearchAsync(string query, string type, int limit = 20, int offset = 0, CancellationToken ct = default);

    /// <summary>获取单曲详情</summary>
    Task<TrackObject> GetTrackAsync(string spotifyId, CancellationToken ct = default);

    /// <summary>批量获取曲目（最多 50 个）</summary>
    Task<List<TrackObject>> GetTracksAsync(IReadOnlyList<string> spotifyIds, CancellationToken ct = default);

    /// <summary>获取艺术家详情</summary>
    Task<ArtistObject> GetArtistAsync(string spotifyId, CancellationToken ct = default);

    /// <summary>获取专辑详情</summary>
    Task<AlbumObject> GetAlbumAsync(string spotifyId, CancellationToken ct = default);

    /// <summary>获取单曲音频特征</summary>
    Task<AudioFeaturesObject?> GetAudioFeaturesAsync(string spotifyId, CancellationToken ct = default);

    /// <summary>批量获取音频特征（最多 100 个）</summary>
    Task<List<AudioFeaturesObject>> GetAudioFeaturesBatchAsync(IReadOnlyList<string> spotifyIds, CancellationToken ct = default);

    /// <summary>获取艺术家热门曲目</summary>
    Task<List<TrackObject>> GetArtistTopTracksAsync(string spotifyId, string? market = null, CancellationToken ct = default);

    /// <summary>获取可用流派种子列表（用于推荐 API 的 seed_genres 参数）</summary>
    Task<AvailableGenresResponse> GetAvailableGenresAsync(CancellationToken ct = default);

    /// <summary>基于种子流派获取推荐曲目（最多 5 个种子流派）</summary>
    Task<RecommendationsResponse> GetRecommendationsAsync(
        List<string> seedGenres, int limit = 20, CancellationToken ct = default);

    // ─── 播放控制 API（需要 user-modify-playback-state / user-read-playback-state scope）───

    /// <summary>获取当前播放状态（含设备、进度、当前曲目）</summary>
    Task<PlaybackStateObject?> GetPlaybackStateAsync(CancellationToken ct = default);

    /// <summary>获取可用设备列表</summary>
    Task<List<DeviceObject>> GetAvailableDevicesAsync(CancellationToken ct = default);

    /// <summary>开始/恢复播放（可指定曲目 URI 列表或上下文 URI）</summary>
    Task StartPlaybackAsync(string? deviceId = null, IReadOnlyList<string>? uris = null,
        string? contextUri = null, int? positionMs = null, CancellationToken ct = default);

    /// <summary>暂停播放</summary>
    Task PausePlaybackAsync(string? deviceId = null, CancellationToken ct = default);

    /// <summary>切换到下一首</summary>
    Task SkipToNextAsync(string? deviceId = null, CancellationToken ct = default);

    /// <summary>切换到上一首</summary>
    Task SkipToPreviousAsync(string? deviceId = null, CancellationToken ct = default);

    /// <summary>设置音量（0-100）</summary>
    Task SetVolumeAsync(int volumePercent, string? deviceId = null, CancellationToken ct = default);

    /// <summary>跳转到指定位置（毫秒）</summary>
    Task SeekToPositionAsync(int positionMs, string? deviceId = null, CancellationToken ct = default);

    /// <summary>设置重复模式（off / context / track）</summary>
    Task SetRepeatModeAsync(string state, string? deviceId = null, CancellationToken ct = default);

    /// <summary>设置随机播放开关</summary>
    Task SetShuffleAsync(bool state, string? deviceId = null, CancellationToken ct = default);

    /// <summary>转移播放到指定设备</summary>
    Task TransferPlaybackAsync(string deviceId, bool play = false, CancellationToken ct = default);
}
