using MediatR;
using MusicRec.Playlists.DTOs;

namespace MusicRec.Playlists.Queries;

/// <summary>获取用户的所有歌单 — 按更新时间降序</summary>
public record GetUserPlaylistsQuery(Guid UserId) : IRequest<IReadOnlyList<PlaylistDto>>;

/// <summary>获取歌单详情 — CurrentUserId 为 null 时仅允许查看公开歌单</summary>
public record GetPlaylistDetailQuery(Guid PlaylistId, Guid? CurrentUserId = null)
    : IRequest<PlaylistDetailDto>;
