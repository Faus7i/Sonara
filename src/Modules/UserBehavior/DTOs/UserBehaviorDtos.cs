namespace MusicRec.UserBehavior.DTOs;

/// <summary>
/// 播放历史条目 DTO — 展平 Track + Artist + Album 信息
/// </summary>
public record PlayHistoryDto(
    Guid Id,
    Guid TrackId,
    string SpotifyTrackId,
    string TrackName,
    string? CoverImageUrl,
    int DurationMs,
    string ArtistsSummary,
    string? AlbumName,
    DateTime PlayedAt,
    int DurationPlayed,
    bool Completed,
    string? Source
);

/// <summary>
/// 用户行为统计摘要
/// </summary>
public record UserBehaviorStatsDto(
    int TotalPlayCount,
    int TotalSkipCount,
    int TotalCompleteCount,
    double SkipRate,
    int Last7DaysPlayCount,
    int Last30DaysPlayCount,
    int FavoriteHour,
    string? TopSource
);

/// <summary>
/// 用户偏好画像 DTO — 供前端展示和 Phase 5 推荐算法使用
/// </summary>
public record UserProfileDto(
    Guid UserId,
    string? FavoriteGenres,
    double AvgEnergy,
    double AvgDanceability,
    double AvgValence,
    double AvgTempo,
    double AvgAcousticness,
    string? TopArtists,
    string? TopTracks,
    double ExplorationLevel,
    int TotalPlayCount,
    DateTime LastUpdatedAt
);
