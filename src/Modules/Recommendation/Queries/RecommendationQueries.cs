using MediatR;
using MusicRec.Recommendation.DTOs;

namespace MusicRec.Recommendation.Queries;

/// <summary>
/// 个性化推荐查询 — 基于用户画像、播放历史、收藏计算混合推荐
/// </summary>
public record GetRecommendationsQuery(Guid UserId, int Limit = 20)
    : IRequest<IReadOnlyList<RecommendationResultDto>>;

/// <summary>
/// 相似曲目查询 — 基于音频特征余弦相似度 + 流派/艺术家重叠度查找相似曲目
/// </summary>
public record GetSimilarTracksQuery(Guid UserId, Guid TrackId, int Limit = 10)
    : IRequest<IReadOnlyList<RecommendationResultDto>>;
