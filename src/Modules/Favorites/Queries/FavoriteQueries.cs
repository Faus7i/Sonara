using MediatR;
using MusicRec.Favorites.DTOs;

namespace MusicRec.Favorites.Queries;

/// <summary>
/// 获取用户收藏列表 — 支持分页，按收藏时间降序
/// </summary>
public record GetUserLikesQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<GetUserLikesResult>;

/// <summary>
/// 检查是否已收藏某曲目 — 供前端渲染红心图标
/// </summary>
public record CheckLikeQuery(Guid UserId, Guid TrackId) : IRequest<bool>;

/// <summary>
/// 收藏列表分页结果 — 包含分页元数据供前端分页组件使用
/// </summary>
public record GetUserLikesResult(
    IReadOnlyList<FavoriteTrackDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
