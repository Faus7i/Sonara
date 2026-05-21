using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MusicRec.Discovery.DTOs;
using MusicRec.Discovery.Queries;
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
}
