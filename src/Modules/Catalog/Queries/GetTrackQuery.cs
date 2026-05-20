using MediatR;
using MusicRec.Catalog.DTOs;

namespace MusicRec.Catalog.Queries;

/// <summary>
/// 查询曲目详情（先查本地 DB，未命中则从 Spotify 导入）
/// </summary>
public record GetTrackQuery(Guid TrackId) : IRequest<TrackDto>;
