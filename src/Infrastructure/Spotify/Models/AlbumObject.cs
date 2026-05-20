using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 专辑完整对象
/// </summary>
public class AlbumObject
{
    [JsonPropertyName("album_type")]
    public string AlbumType { get; set; } = string.Empty;

    [JsonPropertyName("total_tracks")]
    public int TotalTracks { get; set; }

    [JsonPropertyName("available_markets")]
    public List<string>? AvailableMarkets { get; set; }

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    public List<ImageObject> Images { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("release_date_precision")]
    public string ReleaseDatePrecision { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("artists")]
    public List<SimplifiedArtistObject> Artists { get; set; } = new();

    [JsonPropertyName("tracks")]
    public PaginatedResponse<SimplifiedTrackObject> Tracks { get; set; } = new();

    [JsonPropertyName("copyrights")]
    public List<CopyrightObject> Copyrights { get; set; } = new();

    [JsonPropertyName("external_ids")]
    public ExternalIds? ExternalIds { get; set; }

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }
}

/// <summary>
/// 简化曲目对象（嵌入在专辑的 tracks 分页中）
/// </summary>
public class SimplifiedTrackObject
{
    [JsonPropertyName("artists")]
    public List<SimplifiedArtistObject> Artists { get; set; } = new();

    [JsonPropertyName("available_markets")]
    public List<string>? AvailableMarkets { get; set; }

    [JsonPropertyName("disc_number")]
    public int DiscNumber { get; set; }

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; }

    [JsonPropertyName("explicit")]
    public bool Explicit { get; set; }

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; set; }

    [JsonPropertyName("track_number")]
    public int TrackNumber { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("is_local")]
    public bool IsLocal { get; set; }
}

public class CopyrightObject
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
