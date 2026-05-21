namespace MusicRec.Favorites.DTOs;

/// <summary>
/// 收藏记录 DTO — 收藏操作的最小响应
/// </summary>
public record UserLikeDto(Guid Id, Guid UserId, Guid TrackId, DateTime CreatedAt);

/// <summary>
/// 收藏曲目详情 DTO — 展平 Track + Artist + Album 信息供前端列表展示
/// </summary>
/// <remarks>
/// 由 GetUserLikesQueryHandler 通过 JOIN Tracks/Artists/Albums 表构建，
/// 避免前端拿到 TrackId 后再逐一请求曲目详情（N+1 问题）。
/// </remarks>
public record FavoriteTrackDto(
    Guid LikeId,
    Guid TrackId,
    string SpotifyTrackId,
    string Name,
    string? CoverImageUrl,
    int DurationMs,
    string ArtistsSummary,
    string? AlbumName,
    DateTime LikedAt
);
