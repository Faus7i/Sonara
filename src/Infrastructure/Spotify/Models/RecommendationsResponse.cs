using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 推荐 API 响应 — GET /v1/recommendations
/// </summary>
public class RecommendationsResponse
{
    [JsonPropertyName("tracks")]
    public List<TrackObject> Tracks { get; set; } = new();

    [JsonPropertyName("seeds")]
    public List<RecommendationSeedObject> Seeds { get; set; } = new();
}

/// <summary>
/// 推荐 API 的种子信息
/// </summary>
public class RecommendationSeedObject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// 可用流派种子响应 — GET /v1/recommendations/available-genre-seeds
/// </summary>
public class AvailableGenresResponse
{
    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();
}
