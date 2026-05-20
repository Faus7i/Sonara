using MediatR;
using MusicRec.Catalog.DTOs;

namespace MusicRec.Catalog.Commands;

/// <summary>
/// 从 Spotify 导入曲目（含关联的艺术家/专辑/流派数据）
/// </summary>
public record ImportTrackCommand(string SpotifyTrackId) : IRequest<TrackDto>;
