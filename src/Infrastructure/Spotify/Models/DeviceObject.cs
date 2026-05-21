using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 设备对象 — 表示用户可用的播放设备
/// </summary>
public class DeviceObject
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("is_private_session")]
    public bool IsPrivateSession { get; set; }

    [JsonPropertyName("is_restricted")]
    public bool IsRestricted { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("volume_percent")]
    public int? VolumePercent { get; set; }
}

/// <summary>
/// Spotify 设备列表 API 响应包装
/// </summary>
public class DevicesResponse
{
    [JsonPropertyName("devices")]
    public List<DeviceObject> Devices { get; set; } = new();
}
