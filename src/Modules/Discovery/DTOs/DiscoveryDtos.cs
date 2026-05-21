namespace MusicRec.Discovery.DTOs;

/// <summary>
/// 探索推荐结果 DTO — 含探索原因，供前端展示发现页面
/// 与 RecommendationResultDto 区分：无 Score 字段，使用 DiscoveryReason
/// </summary>
public record DiscoveryResultDto(
    Guid TrackId,
    string TrackName,
    string? CoverImageUrl,
    int DurationMs,
    int Popularity,
    string ArtistsSummary,
    string? AlbumName,
    string DiscoveryReason,
    IReadOnlyList<string> Genres
);
