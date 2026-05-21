using MediatR;
using MusicRec.UserBehavior.DTOs;

namespace MusicRec.UserBehavior.Queries;

/// <summary>
/// 获取播放历史 — 支持分页，按播放时间降序
/// </summary>
public record GetPlayHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<PlayHistoryResult>;

/// <summary>
/// 获取行为统计摘要 — 播放次数、跳过率、偏好时段等
/// </summary>
public record GetUserBehaviorStatsQuery(Guid UserId) : IRequest<UserBehaviorStatsDto>;

/// <summary>
/// 获取用户画像 — 不存在则返回默认空画像
/// </summary>
public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;

/// <summary>
/// 播放历史分页结果
/// </summary>
public record PlayHistoryResult(
    IReadOnlyList<PlayHistoryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
