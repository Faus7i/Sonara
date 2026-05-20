using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 分页信封 — 所有列表端点的通用包装
/// </summary>
public class PaginatedResponse<T>
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();
}
