using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Shared;
using MusicRec.UserBehavior.Commands;
using MusicRec.UserBehavior.DTOs;
using MusicRec.UserBehavior.Queries;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 用户行为控制器 — 播放记录、行为事件采集、用户画像
/// </summary>
/// <remarks>
/// 行为数据写入为非关键操作（规则 12），Handler 内部捕获异常不抛到 Controller 层。
/// 所有端点需要 JWT 认证。
/// </remarks>
[ApiController]
[Route("api/user-behavior")]
[Authorize]
[Produces("application/json")]
public class UserBehaviorController : ControllerBase
{
    private readonly ISender _sender;

    public UserBehaviorController(ISender sender) => _sender = sender;

    /// <summary>记录开始播放，返回播放历史 ID 供后续 finish/skip 使用</summary>
    [HttpPost("play")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> RecordPlay(Guid trackId, string? source = null)
    {
        var userId = User.GetUserId();
        var playHistoryId = await _sender.Send(new RecordPlayCommand(userId, trackId, source));
        return Ok(ApiResponse<Guid>.Ok(playHistoryId, "播放已记录"));
    }

    /// <summary>记录播放完成</summary>
    [HttpPut("play/{id:guid}/finish")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> FinishPlay(Guid id, int durationPlayed)
    {
        var userId = User.GetUserId();
        await _sender.Send(new FinishPlayCommand(userId, id, durationPlayed));
        return Ok(ApiResponse.Ok("播放完成已记录"));
    }

    /// <summary>记录跳过</summary>
    [HttpPut("play/{id:guid}/skip")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SkipPlay(Guid id, int skippedAtPositionMs)
    {
        var userId = User.GetUserId();
        await _sender.Send(new SkipPlayCommand(userId, id, skippedAtPositionMs));
        return Ok(ApiResponse.Ok("跳过已记录"));
    }

    /// <summary>记录通用行为事件（点击/停留/滚动）</summary>
    [HttpPost("events")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RecordEvent(
        string eventType, Guid? trackId = null, string? context = null,
        int? duration = null, string? metadata = null)
    {
        var userId = User.GetUserId();
        await _sender.Send(new RecordBehaviorEventCommand(userId, trackId, eventType, context, duration, metadata));
        return Ok(ApiResponse.Ok("行为事件已记录"));
    }

    /// <summary>获取播放历史（分页）</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<PlayHistoryResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlayHistoryResult>>> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetPlayHistoryQuery(userId, page, pageSize));
        return Ok(ApiResponse<PlayHistoryResult>.Ok(result));
    }

    /// <summary>获取行为统计摘要</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<UserBehaviorStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserBehaviorStatsDto>>> GetStats()
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetUserBehaviorStatsQuery(userId));
        return Ok(ApiResponse<UserBehaviorStatsDto>.Ok(result));
    }

    /// <summary>获取用户偏好画像</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetUserProfileQuery(userId));
        return Ok(ApiResponse<UserProfileDto>.Ok(result));
    }

    /// <summary>手动刷新用户画像</summary>
    [HttpPost("profile/refresh")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> RefreshProfile()
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new RefreshUserProfileCommand(userId));
        return Ok(ApiResponse<UserProfileDto>.Ok(result, "画像已刷新"));
    }
}
