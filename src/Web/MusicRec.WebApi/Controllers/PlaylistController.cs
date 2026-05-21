using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Playlists.Commands;
using MusicRec.Playlists.DTOs;
using MusicRec.Playlists.Queries;
using MusicRec.Shared;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 歌单控制器 — 歌单 CRUD + 曲目添加/移除/排序
/// </summary>
/// <remarks>
/// 所有权保护：所有歌单写操作验证 Playlist.UserId == 当前登录用户，
/// 非所有者返回 403。歌单详情对公开歌单免鉴权，私有歌单仅所有者可见。
/// </remarks>
[ApiController]
[Route("api/playlists")]
[Authorize]
[Produces("application/json")]
public class PlaylistController : ControllerBase
{
    private readonly ISender _sender;

    public PlaylistController(ISender sender) => _sender = sender;

    /// <summary>获取当前用户的所有歌单</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlaylistDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlaylistDto>>>> GetList()
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new GetUserPlaylistsQuery(userId));
        return Ok(ApiResponse<IReadOnlyList<PlaylistDto>>.Ok(result));
    }

    /// <summary>获取歌单详情（含曲目列表）</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlaylistDetailDto>>> GetDetail(Guid id)
    {
        var userId = User.GetUserIdOrNull();
        var result = await _sender.Send(new GetPlaylistDetailQuery(id, userId));
        return Ok(ApiResponse<PlaylistDetailDto>.Ok(result));
    }

    /// <summary>创建歌单</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PlaylistDto>>> Create(
        [FromBody] CreatePlaylistRequest request)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new CreatePlaylistCommand(
            userId, request.Name, request.Description, request.IsPublic));
        return Ok(ApiResponse<PlaylistDto>.Ok(result, "歌单创建成功"));
    }

    /// <summary>编辑歌单（部分更新）</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlaylistDto>>> Update(
        Guid id, [FromBody] UpdatePlaylistRequest request)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new UpdatePlaylistCommand(
            id, userId, request.Name, request.Description,
            request.CoverImageUrl, request.IsPublic));
        return Ok(ApiResponse<PlaylistDto>.Ok(result, "歌单已更新"));
    }

    /// <summary>删除歌单</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var userId = User.GetUserId();
        await _sender.Send(new DeletePlaylistCommand(id, userId));
        return Ok(ApiResponse.Ok("歌单已删除"));
    }

    /// <summary>向歌单添加曲目</summary>
    [HttpPost("{id:guid}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistTrackItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PlaylistTrackItemDto>>> AddTrack(
        Guid id, [FromBody] AddTrackToPlaylistRequest request)
    {
        var userId = User.GetUserId();
        var result = await _sender.Send(new AddTrackToPlaylistCommand(id, userId, request.TrackId));
        return Ok(ApiResponse<PlaylistTrackItemDto>.Ok(result, "曲目已添加到歌单"));
    }

    /// <summary>从歌单移除曲目</summary>
    [HttpDelete("{playlistId:guid}/tracks/{trackId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> RemoveTrack(
        Guid playlistId, Guid trackId)
    {
        var userId = User.GetUserId();
        await _sender.Send(new RemoveTrackFromPlaylistCommand(playlistId, userId, trackId));
        return Ok(ApiResponse.Ok("曲目已从歌单移除"));
    }

    /// <summary>重新排序歌单曲目</summary>
    [HttpPut("{id:guid}/tracks/reorder")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> ReorderTracks(
        Guid id, [FromBody] ReorderPlaylistTracksRequest request)
    {
        var userId = User.GetUserId();
        await _sender.Send(new ReorderPlaylistTracksCommand(id, userId, request.TrackIds));
        return Ok(ApiResponse.Ok("歌单顺序已更新"));
    }
}
