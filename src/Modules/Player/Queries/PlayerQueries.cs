using MediatR;
using MusicRec.Player.DTOs;

namespace MusicRec.Player.Queries;

/// <summary>获取当前播放状态 — 无活跃播放时返回 null</summary>
public record GetPlaybackStateQuery : IRequest<PlaybackStateDto?>;

/// <summary>获取可用设备列表 — 空列表表示无可用设备</summary>
public record GetAvailableDevicesQuery : IRequest<IReadOnlyList<DeviceDto>>;
