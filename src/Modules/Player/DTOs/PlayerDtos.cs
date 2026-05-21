namespace MusicRec.Player.DTOs;

// ─── 查询响应 DTO ──────────────────────────────────────

/// <summary>
/// 播放状态 DTO — 从 Spotify PlaybackStateObject 映射，供前端播放器 UI 使用
/// </summary>
public record PlaybackStateDto(
    bool IsPlaying,
    int? ProgressMs,
    string RepeatState,
    bool ShuffleState,
    string? ContextType,
    string? ContextUri,
    TrackBriefDto? CurrentTrack,
    DeviceDto? Device
);

/// <summary>
/// 当前播放曲目摘要 — 仅包含前端播放器需要展示的字段
/// </summary>
public record TrackBriefDto(
    string SpotifyId,
    string Name,
    string ArtistsSummary,
    string? AlbumName,
    string? CoverImageUrl,
    int DurationMs
);

/// <summary>
/// 设备信息 DTO
/// </summary>
public record DeviceDto(
    string? Id,
    string Name,
    string Type,
    bool IsActive,
    int? VolumePercent
);

// ─── 请求 DTO（Controller FromBody 入参）────────────────

/// <summary>
/// 开始播放请求 — 支持指定曲目列表、上下文 URI、起始位置和设备
/// </summary>
public record StartPlaybackRequest(
    string? DeviceId = null,
    IReadOnlyList<string>? Uris = null,
    string? ContextUri = null,
    int? PositionMs = null
);

/// <summary>
/// 转移播放到指定设备请求
/// </summary>
public record TransferPlaybackRequest(string DeviceId, bool Play = false);
