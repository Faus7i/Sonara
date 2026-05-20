using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 艺术家完整对象
/// </summary>
public class ArtistObject
{
    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("followers")]
    public Followers Followers { get; set; } = new();

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    public List<ImageObject> Images { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

public class Followers
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
