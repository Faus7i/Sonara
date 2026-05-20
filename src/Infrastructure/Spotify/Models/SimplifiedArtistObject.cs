using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 简化艺术家对象（嵌入在 TrackObject 等中）
/// </summary>
public class SimplifiedArtistObject
{
    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

public class ExternalUrls
{
    [JsonPropertyName("spotify")]
    public string Spotify { get; set; } = string.Empty;
}
