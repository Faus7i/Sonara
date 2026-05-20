using MediatR;
using MusicRec.Catalog.DTOs;

namespace MusicRec.Catalog.Commands;

/// <summary>
/// 从 Spotify 导入艺术家及其热门曲目
/// </summary>
public record ImportArtistCommand(string SpotifyArtistId) : IRequest<ArtistDto>;
