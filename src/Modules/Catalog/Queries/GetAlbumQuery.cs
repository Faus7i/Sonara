using MediatR;
using MusicRec.Catalog.DTOs;

namespace MusicRec.Catalog.Queries;

/// <summary>
/// 查询专辑详情（含曲目列表）
/// </summary>
public record GetAlbumQuery(Guid AlbumId) : IRequest<AlbumDto>;
