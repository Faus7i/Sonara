using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Favorites.Commands;
using MusicRec.Favorites.DTOs;
using MusicRec.Favorites.Queries;
using MusicRec.Shared;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 收藏控制器 — 管理用户曲目收藏（添加/移除/列表/检查状态）
/// </summary>
/// <remarks>
/// 所有端点需要 JWT 认证，UserId 从 Token 的 sub claim 自动提取。
/// 收藏操作均为幂等设计——重复收藏不报错，重复取消静默成功。
/// </remarks>
[ApiController]
[Route("api/favorites")]
[Authorize]
[Produces("application/json")]
public class FavoritesController : ControllerBase
{
    private readonly ISender _sender;

    public FavoritesController(ISender sender) => _sender = sender;

    /// <summary>获取收藏列表（分页）</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetUserLikesResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GetUserLikesResult>>> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetUserLikesQuery(userId, page, pageSize));
        return Ok(ApiResponse<GetUserLikesResult>.Ok(result));
    }

    /// <summary>收藏曲目</summary>
    [HttpPost("tracks/{trackId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserLikeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserLikeDto>>> Like(Guid trackId)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new LikeTrackCommand(userId, trackId));
        return Ok(ApiResponse<UserLikeDto>.Ok(result, "收藏成功"));
    }

    /// <summary>取消收藏</summary>
    [HttpDelete("tracks/{trackId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Unlike(Guid trackId)
    {
        var userId = User.GetUserId();
        await _sender.Send(new UnlikeTrackCommand(userId, trackId));
        return Ok(ApiResponse.Ok("已取消收藏"));
    }

    /// <summary>检查是否已收藏某曲目</summary>
    [HttpGet("check/{trackId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Check(Guid trackId)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new CheckLikeQuery(userId, trackId));
        return Ok(ApiResponse<bool>.Ok(result));
    }
}
