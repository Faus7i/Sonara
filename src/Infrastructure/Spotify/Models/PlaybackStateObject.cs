using System.Text.Json.Serialization;

namespace MusicRec.Spotify.Models;

/// <summary>
/// Spotify 播放状态完整对象 — 包含设备、进度、当前曲目等
/// </summary>
public class PlaybackStateObject
{
    [JsonPropertyName("device")]
    public DeviceObject? Device { get; set; }

    [JsonPropertyName("repeat_state")]
    public string RepeatState { get; set; } = string.Empty;

    [JsonPropertyName("shuffle_state")]
    public bool ShuffleState { get; set; }

    [JsonPropertyName("context")]
    public PlaybackContextObject? Context { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("progress_ms")]
    public int? ProgressMs { get; set; }

    [JsonPropertyName("is_playing")]
    public bool IsPlaying { get; set; }

    [JsonPropertyName("item")]
    public TrackObject? Item { get; set; }

    [JsonPropertyName("currently_playing_type")]
    public string CurrentlyPlayingType { get; set; } = string.Empty;

    [JsonPropertyName("actions")]
    public PlaybackActions? Actions { get; set; }
}

/// <summary>
/// 播放器支持的操作 — Spotify API 返回 actions.disallows.{property} 嵌套结构
/// </summary>
public class PlaybackActions
{
    [JsonPropertyName("disallows")]
    public PlaybackDisallows? Disallows { get; set; }
}

/// <summary>
/// 播放器禁用的操作列表（true = 该操作当前不可用）
/// </summary>
public class PlaybackDisallows
{
    [JsonPropertyName("interrupting_playback")]
    public bool? InterruptingPlayback { get; set; }

    [JsonPropertyName("pausing")]
    public bool? Pausing { get; set; }

    [JsonPropertyName("resuming")]
    public bool? Resuming { get; set; }

    [JsonPropertyName("seeking")]
    public bool? Seeking { get; set; }

    [JsonPropertyName("skipping_next")]
    public bool? SkippingNext { get; set; }

    [JsonPropertyName("skipping_prev")]
    public bool? SkippingPrev { get; set; }

    [JsonPropertyName("toggling_repeat_context")]
    public bool? TogglingRepeatContext { get; set; }

    [JsonPropertyName("toggling_shuffle")]
    public bool? TogglingShuffle { get; set; }

    [JsonPropertyName("toggling_repeat_track")]
    public bool? TogglingRepeatTrack { get; set; }

    [JsonPropertyName("transferring_playback")]
    public bool? TransferringPlayback { get; set; }
}
