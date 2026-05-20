namespace MusicRec.Catalog.DTOs;

/// <summary>
/// 艺术家详情 DTO
/// </summary>
public record ArtistDto(
    Guid Id,
    string SpotifyArtistId,
    string Name,
    string? Genres,
    string? ImageUrl,
    int Popularity
);
