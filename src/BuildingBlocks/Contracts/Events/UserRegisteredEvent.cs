using MediatR;

namespace MusicRec.Contracts.Events;

/// <summary>
/// 用户注册成功事件 — 跨模块通信（后续模块订阅此事件来初始化用户画像等）
/// </summary>
public record UserRegisteredEvent(Guid UserId, string Email, string Nickname) : INotification;
