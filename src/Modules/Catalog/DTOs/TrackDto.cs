namespace MusicRec.Catalog.DTOs;

/// <summary>
/// 曲目详情 DTO — 含艺术家和专辑信息
/// </summary>
public record TrackDto(
    Guid Id,
    string SpotifyTrackId,
    string Name,
    int DurationMs,
    int Popularity,
    string? ReleaseDate,
    string? CoverImageUrl,
    AlbumBriefDto Album,
    IReadOnlyList<ArtistBriefDto> Artists
);

public record ArtistBriefDto(
    Guid Id,
    string SpotifyArtistId,
    string Name,
    string? Genres,
    string? ImageUrl
);

public record AlbumBriefDto(
    Guid Id,
    string SpotifyAlbumId,
    string Name,
    string? ReleaseDate,
    string? CoverImageUrl,
    string? AlbumType
);
