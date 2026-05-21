namespace MusicRec.Playlists.DTOs;

/// <summary>歌单摘要 DTO — 用于列表展示</summary>
public record PlaylistDto(
    Guid Id,
    string Name,
    string? Description,
    string? CoverImageUrl,
    bool IsPublic,
    int TrackCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>歌单详情 DTO — 含完整曲目列表</summary>
public record PlaylistDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? CoverImageUrl,
    bool IsPublic,
    IReadOnlyList<PlaylistTrackItemDto> Tracks,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>歌单中单个曲目条目 DTO — 含曲目摘要信息</summary>
public record PlaylistTrackItemDto(
    Guid Id,
    int OrderIndex,
    DateTime AddedAt,
    Guid TrackId,
    string Name,
    string? CoverImageUrl,
    int DurationMs,
    string ArtistsSummary
);

// ─── 请求 DTO（Controller FromBody 入参，与 MediatR Command 分离）───

/// <summary>创建歌单请求</summary>
public record CreatePlaylistRequest(string Name, string? Description = null, bool IsPublic = false);

/// <summary>编辑歌单请求 — 所有字段可选（部分更新）</summary>
public record UpdatePlaylistRequest(
    string? Name = null,
    string? Description = null,
    string? CoverImageUrl = null,
    bool? IsPublic = null
);

/// <summary>向歌单添加曲目请求</summary>
public record AddTrackToPlaylistRequest(Guid TrackId);

/// <summary>重新排序请求 — 传入新的 TrackId 顺序列表</summary>
public record ReorderPlaylistTracksRequest(IReadOnlyList<Guid> TrackIds);
