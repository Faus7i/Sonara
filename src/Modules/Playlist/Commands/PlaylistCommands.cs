using MediatR;
using MusicRec.Playlists.DTOs;

namespace MusicRec.Playlists.Commands;

/// <summary>创建歌单</summary>
public record CreatePlaylistCommand(Guid UserId, string Name, string? Description, bool IsPublic)
    : IRequest<PlaylistDto>;

/// <summary>编辑歌单 — 仅更新提供的非 null 字段（部分更新）</summary>
public record UpdatePlaylistCommand(
    Guid PlaylistId, Guid UserId,
    string? Name, string? Description, string? CoverImageUrl, bool? IsPublic
) : IRequest<PlaylistDto>;

/// <summary>删除歌单 — 关联 PlaylistTrack 由 Cascade 自动删除</summary>
public record DeletePlaylistCommand(Guid PlaylistId, Guid UserId) : IRequest;

/// <summary>向歌单添加曲目 — OrderIndex 由 Handler 自动计算为 max+1</summary>
public record AddTrackToPlaylistCommand(Guid PlaylistId, Guid UserId, Guid TrackId)
    : IRequest<PlaylistTrackItemDto>;

/// <summary>从歌单移除曲目 — 幂等操作</summary>
public record RemoveTrackFromPlaylistCommand(Guid PlaylistId, Guid UserId, Guid TrackId) : IRequest;

/// <summary>重新排序歌单曲目 — TrackIds 为新顺序的 Track Id 列表</summary>
public record ReorderPlaylistTracksCommand(Guid PlaylistId, Guid UserId, IReadOnlyList<Guid> TrackIds) : IRequest;
