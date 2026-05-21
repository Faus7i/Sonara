using MediatR;
using MusicRec.Favorites.DTOs;

namespace MusicRec.Favorites.Commands;

/// <summary>
/// 收藏曲目 — 幂等操作，已收藏则直接返回已有记录（不抛异常）
/// </summary>
public record LikeTrackCommand(Guid UserId, Guid TrackId) : IRequest<UserLikeDto>;

/// <summary>
/// 取消收藏 — 幂等操作，未收藏则静默返回成功
/// </summary>
public record UnlikeTrackCommand(Guid UserId, Guid TrackId) : IRequest;
