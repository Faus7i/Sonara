namespace MusicRec.Catalog.DTOs;

/// <summary>
/// 专辑详情 DTO
/// </summary>
public record AlbumDto(
    Guid Id,
    string SpotifyAlbumId,
    string Name,
    string? ReleaseDate,
    string? CoverImageUrl,
    string? AlbumType,
    int TotalTracks,
    IReadOnlyList<ArtistBriefDto> Artists,
    IReadOnlyList<TrackBriefDto> Tracks
);

public record TrackBriefDto(
    Guid Id,
    string SpotifyTrackId,
    string Name,
    int DurationMs,
    int TrackNumber,
    int Popularity
);
