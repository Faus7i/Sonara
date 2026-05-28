using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Discovery.Commands;
using MusicRec.Discovery.DTOs;
using MusicRec.Discovery.Queries;
using MusicRec.Recommendation.Commands;
using MusicRec.Shared;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 探索发现 API — Spotify 实时推荐 + 冷启动推荐
/// </summary>
[ApiController]
[Route("api/discovery")]
[Produces("application/json")]
public class DiscoveryController : ControllerBase
{
    private readonly ISender _sender;

    public DiscoveryController(ISender sender) => _sender = sender;

    /// <summary>
    /// 探索推荐（需登录）— 随机选取 Spotify 种子流派，获取实时推荐曲目并自动导入
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiscoveryResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiscoveryResultDto>>>> GetDiscovery(
        [FromQuery] int limit = 20)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetSpotifyExploreQuery(userId, limit));
        return Ok(ApiResponse<IReadOnlyList<DiscoveryResultDto>>.Ok(result));
    }

    /// <summary>
    /// 冷启动推荐（无需登录）— 随机选取 Spotify 种子流派，获取实时推荐曲目并自动导入
    /// </summary>
    [HttpGet("cold-start")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiscoveryResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiscoveryResultDto>>>> GetColdStart(
        [FromQuery] int limit = 20)
    {
        var userId = User.GetUserIdOrNull();
        var result = await _sender.Send(new GetSpotifyExploreQuery(userId, limit));
        return Ok(ApiResponse<IReadOnlyList<DiscoveryResultDto>>.Ok(result));
    }

    /// <summary>
    /// 生成种子曲目数据 — 开发用，生成约 140 首本地曲目覆盖 23 个流派
    /// </summary>
    [HttpPost("seed-tracks")]
    [ProducesResponseType(typeof(ApiResponse<SeedTracksResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SeedTracksResult>>> SeedTracks()
    {
        var trackResult = await _sender.Send(new SeedTracksCommand());
        var audioResult = await _sender.Send(new SeedAudioFeaturesCommand());

        return Ok(ApiResponse<SeedTracksResult>.Ok(trackResult,
            $"种子数据生成完成：{trackResult.TracksCreated} 首曲目, " +
            $"{audioResult.FeaturesCreated} 条音频特征, " +
            $"{trackResult.GenresCreated} 流派, " +
            $"{trackResult.ArtistsCreated} 艺术家, " +
            $"{trackResult.AlbumsCreated} 专辑"));
    }
}
