using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Recommendation.Commands;
using MusicRec.Recommendation.DTOs;
using MusicRec.Recommendation.Queries;
using MusicRec.Shared;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 推荐系统 API — 个性化推荐、相似曲目、种子数据生成
/// </summary>
[ApiController]
[Route("api/recommendations")]
[Authorize]
[Produces("application/json")]
public class RecommendationController : ControllerBase
{
    private readonly ISender _sender;

    public RecommendationController(ISender sender) => _sender = sender;

    /// <summary>
    /// 获取个性化推荐列表 — 基于用户画像与混合推荐算法
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecommendationResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecommendationResultDto>>>> GetRecommendations(
        [FromQuery] int limit = 20)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetRecommendationsQuery(userId, limit));
        return Ok(ApiResponse<IReadOnlyList<RecommendationResultDto>>.Ok(result));
    }

    /// <summary>
    /// 获取相似曲目 — 基于音频特征余弦相似度 + 流派/艺术家重叠
    /// </summary>
    [HttpGet("similar/{trackId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecommendationResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecommendationResultDto>>>> GetSimilarTracks(
        Guid trackId, [FromQuery] int limit = 10)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetSimilarTracksQuery(userId, trackId, limit));
        return Ok(ApiResponse<IReadOnlyList<RecommendationResultDto>>.Ok(result));
    }

    /// <summary>
    /// 生成模拟音频特征种子数据 — 为缺少音频特征的曲目基于流派生成 Test Fixture
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(ApiResponse<SeedAudioFeaturesResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SeedAudioFeaturesResult>>> Seed()
    {
        var result = await _sender.Send(new SeedAudioFeaturesCommand());
        return Ok(ApiResponse<SeedAudioFeaturesResult>.Ok(result,
            $"种子数据生成完成：处理 {result.TracksProcessed} 首曲目，创建 {result.FeaturesCreated} 条音频特征"));
    }
}
