using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 曲目完整对象
/// </summary>
public class TrackObject
{
    [JsonPropertyName("album")]
    public SimplifiedAlbumObject Album { get; set; } = new();

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

    [JsonPropertyName("external_ids")]
    public ExternalIds? ExternalIds { get; set; }

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_playable")]
    public bool? IsPlayable { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }

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

/// <summary>
/// 简化专辑对象（嵌入在搜索结果和 TrackObject 中）
/// </summary>
public class SimplifiedAlbumObject
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
}

public class ExternalIds
{
    [JsonPropertyName("isrc")]
    public string? Isrc { get; set; }

    [JsonPropertyName("ean")]
    public string? Ean { get; set; }

    [JsonPropertyName("upc")]
    public string? Upc { get; set; }
}
