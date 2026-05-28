namespace MusicRec.Recommendation.DTOs;

/// <summary>
/// 推荐结果 DTO — 含评分、推荐原因，供前端展示推荐列表
/// </summary>
public record RecommendationResultDto(
    Guid Id,
    string Name,
    string? CoverImageUrl,
    int DurationMs,
    int Popularity,
    string ArtistsSummary,
    string? AlbumName,
    double Score,
    string Reason,
    IReadOnlyList<string> Genres
);
