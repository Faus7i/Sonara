using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 音频特征对象 — 包含 10 个维度的音频分析数据
/// </summary>
public class AudioFeaturesObject
{
    [JsonPropertyName("acousticness")]
    public float Acousticness { get; set; }

    [JsonPropertyName("analysis_url")]
    public string AnalysisUrl { get; set; } = string.Empty;

    [JsonPropertyName("danceability")]
    public float Danceability { get; set; }

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; }

    [JsonPropertyName("energy")]
    public float Energy { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("instrumentalness")]
    public float Instrumentalness { get; set; }

    [JsonPropertyName("key")]
    public int Key { get; set; }

    [JsonPropertyName("liveness")]
    public float Liveness { get; set; }

    [JsonPropertyName("loudness")]
    public float Loudness { get; set; }

    [JsonPropertyName("mode")]
    public int Mode { get; set; }

    [JsonPropertyName("speechiness")]
    public float Speechiness { get; set; }

    [JsonPropertyName("tempo")]
    public float Tempo { get; set; }

    [JsonPropertyName("time_signature")]
    public int TimeSignature { get; set; }

    [JsonPropertyName("track_href")]
    public string TrackHref { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("valence")]
    public float Valence { get; set; }
}
