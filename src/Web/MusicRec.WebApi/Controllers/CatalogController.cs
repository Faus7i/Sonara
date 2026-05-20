using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicRec.Catalog.Commands;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Queries;
using MusicRec.Shared;

namespace MusicRec.WebApi.Controllers;

/// <summary>
/// 音乐目录控制器 — 曲目/艺术家/专辑的查询与导入
/// </summary>
[ApiController]
[Route("api/catalog")]
[Produces("application/json")]
public class CatalogController : ControllerBase
{
    private readonly ISender _sender;

    public CatalogController(ISender sender) => _sender = sender;

    /// <summary>
    /// 获取曲目详情
    /// </summary>
    [HttpGet("tracks/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TrackDto>>> GetTrack(Guid id)
    {
        var result = await _sender.Send(new GetTrackQuery(id));
        return Ok(ApiResponse<TrackDto>.Ok(result));
    }

    /// <summary>
    /// 从 Spotify 导入曲目
    /// </summary>
    [HttpPost("tracks/import")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TrackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TrackDto>>> ImportTrack(
        [FromBody] ImportTrackRequest request)
    {
        var result = await _sender.Send(new ImportTrackCommand(request.SpotifyTrackId));
        return Ok(ApiResponse<TrackDto>.Ok(result, "曲目导入成功"));
    }

    /// <summary>
    /// 获取艺术家详情
    /// </summary>
    [HttpGet("artists/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ArtistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ArtistDto>>> GetArtist(Guid id)
    {
        var result = await _sender.Send(new GetArtistQuery(id));
        return Ok(ApiResponse<ArtistDto>.Ok(result));
    }

    /// <summary>
    /// 从 Spotify 导入艺术家
    /// </summary>
    [HttpPost("artists/import")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ArtistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ArtistDto>>> ImportArtist(
        [FromBody] ImportArtistRequest request)
    {
        var result = await _sender.Send(new ImportArtistCommand(request.SpotifyArtistId));
        return Ok(ApiResponse<ArtistDto>.Ok(result, "艺术家导入成功"));
    }

    /// <summary>
    /// 获取专辑详情
    /// </summary>
    [HttpGet("albums/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AlbumDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AlbumDto>>> GetAlbum(Guid id)
    {
        var result = await _sender.Send(new GetAlbumQuery(id));
        return Ok(ApiResponse<AlbumDto>.Ok(result));
    }
}
