namespace MusicRec.Search.DTOs;

/// <summary>
/// 统一搜索结果 DTO
/// </summary>
public record SearchResultDto(
    IReadOnlyList<SearchTrackDto> Tracks,
    IReadOnlyList<SearchArtistDto> Artists,
    IReadOnlyList<SearchAlbumDto> Albums
);

public record SearchTrackDto(
    string SpotifyTrackId,
    string Name,
    int DurationMs,
    int Popularity,
    string? CoverImageUrl,
    string AlbumName,
    string ArtistsSummary
);

public record SearchArtistDto(
    string SpotifyArtistId,
    string Name,
    string? Genres,
    string? ImageUrl,
    int Popularity
);

public record SearchAlbumDto(
    string SpotifyAlbumId,
    string Name,
    string? ReleaseDate,
    string? CoverImageUrl,
    string? AlbumType,
    string ArtistsSummary
);
