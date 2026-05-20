using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Search.Commands;
using MusicRec.Search.DTOs;
using MusicRec.Shared;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 音乐搜索控制器 — 通过 Spotify API 统一搜索曲目/艺术家/专辑
/// </summary>
[ApiController]
[Route("api/search")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ISender _sender;

    public SearchController(ISender sender) => _sender = sender;

    /// <summary>
    /// 统一搜索 — 支持曲目/艺术家/专辑
    /// </summary>
    /// <param name="q">搜索关键词</param>
    /// <param name="type">搜索类型：track,artist,album（逗号分隔，默认全部）</param>
    /// <param name="limit">每类结果数（1-50，默认 20）</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SearchResultDto>>> Search(
        [FromQuery] string q,
        [FromQuery] string type = "track,artist,album",
        [FromQuery] int limit = 20)
    {
        Guid? userId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim, out var uid))
            userId = uid;

        var result = await _sender.Send(new SearchMusicCommand(q, type, limit, userId));
        return Ok(ApiResponse<SearchResultDto>.Ok(result));
    }
}
