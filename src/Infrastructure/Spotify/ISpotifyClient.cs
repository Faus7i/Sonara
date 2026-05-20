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
}
