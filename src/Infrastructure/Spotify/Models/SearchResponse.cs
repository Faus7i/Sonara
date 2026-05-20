using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 搜索 API 完整响应
/// </summary>
public class SearchResponse
{
    [JsonPropertyName("tracks")]
    public PaginatedResponse<TrackObject>? Tracks { get; set; }

    [JsonPropertyName("artists")]
    public PaginatedResponse<ArtistObject>? Artists { get; set; }

    [JsonPropertyName("albums")]
    public PaginatedResponse<SimplifiedAlbumObject>? Albums { get; set; }
}

/// <summary>
/// Spotify 批量曲目响应
/// </summary>
public class SeveralTracksResponse
{
    [JsonPropertyName("tracks")]
    public List<TrackObject> Tracks { get; set; } = new();
}

/// <summary>
/// Spotify 批量音频特征响应
/// </summary>
public class SeveralAudioFeaturesResponse
{
    [JsonPropertyName("audio_features")]
    public List<AudioFeaturesObject> AudioFeatures { get; set; } = new();
}

/// <summary>
/// 艺术家热门曲目响应
/// </summary>
public class ArtistTopTracksResponse
{
    [JsonPropertyName("tracks")]
    public List<TrackObject> Tracks { get; set; } = new();
}
