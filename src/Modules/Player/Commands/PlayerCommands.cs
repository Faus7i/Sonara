using MediatR;

namespace MusicRec.Player.Commands;

/// <summary>开始/恢复播放 — 可指定曲目 URI、上下文、起始位置和设备</summary>
public record StartPlaybackCommand(
    string? DeviceId = null,
    IReadOnlyList<string>? Uris = null,
    string? ContextUri = null,
    int? PositionMs = null
) : IRequest;

/// <summary>暂停当前播放</summary>
public record PausePlaybackCommand(string? DeviceId = null) : IRequest;

/// <summary>切换到下一首</summary>
public record SkipToNextCommand(string? DeviceId = null) : IRequest;

/// <summary>切换到上一首</summary>
public record SkipToPreviousCommand(string? DeviceId = null) : IRequest;

/// <summary>设置音量（0-100，Spotify API 范围）</summary>
public record SetVolumeCommand(int VolumePercent, string? DeviceId = null) : IRequest;

/// <summary>跳转到指定播放位置</summary>
public record SeekToPositionCommand(int PositionMs, string? DeviceId = null) : IRequest;

/// <summary>设置重复模式（off / context / track）</summary>
public record SetRepeatModeCommand(string State, string? DeviceId = null) : IRequest;

/// <summary>设置随机播放开关</summary>
public record SetShuffleCommand(bool State, string? DeviceId = null) : IRequest;

/// <summary>转移播放到指定设备</summary>
public record TransferPlaybackCommand(string DeviceId, bool Play = false) : IRequest;
