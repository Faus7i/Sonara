using MediatR;

namespace MusicRec.Contracts.Events;

/// <summary>
/// 用户行为画像更新事件 — 当行为数据变化导致画像重新计算后发布
/// Phase 5 的 Recommendation 模块订阅此事件以刷新推荐列表
/// </summary>
public record UserBehaviorUpdatedEvent(Guid UserId) : INotification;
