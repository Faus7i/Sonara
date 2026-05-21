using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Identity.Commands;
using MusicRec.Identity.DTOs;
using MusicRec.Identity.Queries;
using MusicRec.Shared;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 用户系统接口 — 注册、登录、资料管理
/// </summary>
[ApiController]
[Route("api/identity")]
[Produces("application/json")]
public class IdentityController : ControllerBase
{
    private readonly ISender _sender;

    public IdentityController(ISender sender) => _sender = sender;

    /// <summary>
    /// 用户注册 — 成功后自动登录并返回 JWT
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _sender.Send(
            new RegisterUserCommand(request.Email, request.Password, request.Nickname));
        return Ok(ApiResponse<AuthResultDto>.Ok(result, "注册成功"));
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Login([FromBody] LoginRequest request)
    {
        var result = await _sender.Send(new LoginUserCommand(request.Email, request.Password));
        return Ok(ApiResponse<AuthResultDto>.Ok(result, "登录成功"));
    }

    /// <summary>
    /// 获取当前登录用户的资料
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetUserProfileQuery(userId));
        return Ok(ApiResponse<UserProfileDto>.Ok(result));
    }

    /// <summary>
    /// 更新当前登录用户的资料（昵称、头像）
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(
            new UpdateProfileCommand(userId, request.Nickname, request.AvatarUrl));
        return Ok(ApiResponse<UserProfileDto>.Ok(result, "资料更新成功"));
    }
}
