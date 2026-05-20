using MediatR;
using MusicRec.Catalog.DTOs;

namespace MusicRec.Catalog.Queries;

/// <summary>
/// 查询艺术家详情
/// </summary>
public record GetArtistQuery(Guid ArtistId) : IRequest<ArtistDto>;
