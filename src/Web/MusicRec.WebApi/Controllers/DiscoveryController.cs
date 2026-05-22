using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MusicRec.Discovery.Commands;
using MusicRec.Discovery.DTOs;
using MusicRec.Discovery.Queries;
using MusicRec.Recommendation.Commands;
using MusicRec.Shared;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 探索发现 API — 冷启动推荐、个性化探索推荐
/// </summary>
[ApiController]
[Route("api/discovery")]
[Produces("application/json")]
public class DiscoveryController : ControllerBase
{
    private readonly ISender _sender;

    public DiscoveryController(ISender sender) => _sender = sender;

    /// <summary>
    /// 个性化探索推荐 — 70% 热门 + 20% 偏好流派 + 10% 随机新风格
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiscoveryResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiscoveryResultDto>>>> GetDiscovery(
        [FromQuery] int limit = 20)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetDiscoveryQuery(userId, limit));
        return Ok(ApiResponse<IReadOnlyList<DiscoveryResultDto>>.Ok(result));
    }

    /// <summary>
    /// 冷启动推荐 — 面向新用户/未登录用户，热度 + 流派多样性
    /// </summary>
    [HttpGet("cold-start")]
    [OutputCache(Duration = 1800, VaryByQueryKeys = ["limit"])]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiscoveryResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiscoveryResultDto>>>> GetColdStart(
        [FromQuery] int limit = 20)
    {
        var userId = User.GetUserIdOrNull();
        var result = await _sender.Send(new GetColdStartQuery(userId, limit));
        return Ok(ApiResponse<IReadOnlyList<DiscoveryResultDto>>.Ok(result));
    }

    /// <summary>
    /// 生成种子曲目数据 — 为探索页面生成约 140 首曲目，覆盖 23 个流派
    /// 同时自动生成对应的音频特征（通过 SeedAudioFeaturesCommand）
    /// </summary>
    [HttpPost("seed-tracks")]
    [ProducesResponseType(typeof(ApiResponse<SeedTracksResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SeedTracksResult>>> SeedTracks()
    {
        // 1. 创建曲目、艺术家、专辑、流派
        var trackResult = await _sender.Send(new SeedTracksCommand());

        // 2. 为新建曲目生成音频特征（跨模块编排，WebApi 层引用所有模块）
        var audioResult = await _sender.Send(new SeedAudioFeaturesCommand());

        return Ok(ApiResponse<SeedTracksResult>.Ok(trackResult,
            $"种子数据生成完成：{trackResult.TracksCreated} 首曲目, " +
            $"{audioResult.FeaturesCreated} 条音频特征, " +
            $"{trackResult.GenresCreated} 流派, " +
            $"{trackResult.ArtistsCreated} 艺术家, " +
            $"{trackResult.AlbumsCreated} 专辑"));
    }
}
