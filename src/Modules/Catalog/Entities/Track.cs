using MusicRec.Abstractions;

namespace MusicRec.Catalog.Entities;

/// <summary>
/// 曲目实体 — 对应 Spotify Track
/// </summary>
public class Track : IEntity
{
    public Guid Id { get; set; }
    public string SpotifyTrackId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid AlbumId { get; set; }
    public int DurationMs { get; set; }
    public int Popularity { get; set; }
    public string? ReleaseDate { get; set; }
    public string? CoverImageUrl { get; set; }

    // 导航属性
    public Album Album { get; set; } = null!;
    public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
    public ICollection<TrackGenre> TrackGenres { get; set; } = new List<TrackGenre>();
}
