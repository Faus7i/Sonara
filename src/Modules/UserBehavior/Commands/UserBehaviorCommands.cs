using MediatR;
using MusicRec.UserBehavior.DTOs;

namespace MusicRec.UserBehavior.Commands;

/// <summary>
/// 记录播放开始 — 非关键写入，失败不抛异常
/// 返回播放历史 ID，供前端后续调用 finish/skip 端点
/// </summary>
public record RecordPlayCommand(Guid UserId, Guid TrackId, string? Source = null) : IRequest<Guid>;

/// <summary>
/// 记录播放完成 — 幂等操作，重复调用静默成功
/// </summary>
public record FinishPlayCommand(Guid UserId, Guid PlayHistoryId, int DurationPlayed) : IRequest;

/// <summary>
/// 记录跳过 — 幂等操作，同时写入行为事件
/// </summary>
public record SkipPlayCommand(Guid UserId, Guid PlayHistoryId, int SkippedAtPositionMs) : IRequest;

/// <summary>
/// 记录通用行为事件（点击/停留/滚动等）
/// </summary>
public record RecordBehaviorEventCommand(
    Guid UserId, Guid? TrackId, string EventType,
    string? Context = null, int? Duration = null, string? Metadata = null) : IRequest;

/// <summary>
/// 刷新用户画像 — 重新聚合计算偏好数据
/// </summary>
public record RefreshUserProfileCommand(Guid UserId) : IRequest<UserProfileDto>;
