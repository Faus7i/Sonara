using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Player.Commands;
using MusicRec.Player.DTOs;
using MusicRec.Player.Queries;
using MusicRec.Shared;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 播放控制控制器 — 将 Spotify Web API 的播放端点暴露为 REST API
/// </summary>
/// <remarks>
/// 所有端点需要 JWT 认证。底层调用 ISpotifyClient（通过 MediatR Handler），
/// 当前 Client Credentials Token 可能无播放控制权限（需 Premium + user scope），
/// 但 API 结构已就绪，后续扩展 OAuth scope 即可激活。
/// </remarks>
[ApiController]
[Route("api/player")]
[Authorize]
[Produces("application/json")]
public class PlayerController : ControllerBase
{
    private readonly ISender _sender;

    public PlayerController(ISender sender) => _sender = sender;

    /// <summary>获取当前播放状态（设备、进度、曲目）</summary>
    [HttpGet("state")]
    [ProducesResponseType(typeof(ApiResponse<PlaybackStateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlaybackStateDto?>>> GetState()
    {
        var result = await _sender.Send(new GetPlaybackStateQuery());
        return Ok(ApiResponse<PlaybackStateDto?>.Ok(result));
    }

    /// <summary>获取可用设备列表</summary>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceDto>>>> GetDevices()
    {
        var result = await _sender.Send(new GetAvailableDevicesQuery());
        return Ok(ApiResponse<IReadOnlyList<DeviceDto>>.Ok(result));
    }

    /// <summary>开始/恢复播放</summary>
    [HttpPut("play")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> Play([FromBody] StartPlaybackRequest request)
    {
        await _sender.Send(new StartPlaybackCommand(
            request.DeviceId, request.Uris, request.ContextUri, request.PositionMs));
        return Ok(ApiResponse.Ok("播放已开始"));
    }

    /// <summary>暂停播放</summary>
    [HttpPut("pause")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Pause([FromQuery] string? deviceId = null)
    {
        await _sender.Send(new PausePlaybackCommand(deviceId));
        return Ok(ApiResponse.Ok("播放已暂停"));
    }

    /// <summary>切换到下一首</summary>
    [HttpPost("next")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Next([FromQuery] string? deviceId = null)
    {
        await _sender.Send(new SkipToNextCommand(deviceId));
        return Ok(ApiResponse.Ok("已切换到下一首"));
    }

    /// <summary>切换到上一首</summary>
    [HttpPost("previous")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Previous([FromQuery] string? deviceId = null)
    {
        await _sender.Send(new SkipToPreviousCommand(deviceId));
        return Ok(ApiResponse.Ok("已切换到上一首"));
    }

    /// <summary>设置音量（0-100）</summary>
    [HttpPut("volume")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> SetVolume([FromQuery] int percent)
    {
        await _sender.Send(new SetVolumeCommand(percent));
        return Ok(ApiResponse.Ok($"音量已设置为 {percent}%"));
    }

    /// <summary>跳转到指定位置（毫秒）</summary>
    [HttpPut("seek")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> Seek([FromQuery] int positionMs)
    {
        await _sender.Send(new SeekToPositionCommand(positionMs));
        return Ok(ApiResponse.Ok($"已跳转到 {positionMs}ms"));
    }

    /// <summary>设置重复模式（off / context / track）</summary>
    [HttpPut("repeat")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SetRepeat([FromQuery] string state)
    {
        await _sender.Send(new SetRepeatModeCommand(state));
        return Ok(ApiResponse.Ok($"重复模式已设置为 {state}"));
    }

    /// <summary>设置随机播放开关</summary>
    [HttpPut("shuffle")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SetShuffle([FromQuery] bool state)
    {
        await _sender.Send(new SetShuffleCommand(state));
        return Ok(ApiResponse.Ok($"随机播放已{(state ? "开启" : "关闭")}"));
    }

    /// <summary>转移播放到指定设备</summary>
    [HttpPut("device")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> TransferDevice([FromBody] TransferPlaybackRequest request)
    {
        await _sender.Send(new TransferPlaybackCommand(request.DeviceId, request.Play));
        return Ok(ApiResponse.Ok("播放已转移"));
    }
}
