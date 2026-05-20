using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 图片对象（专辑封面、艺术家图片等）
/// </summary>
public class ImageObject
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }
}
