using MusicRec.Abstractions;

namespace MusicRec.Catalog.Entities;

/// <summary>
/// 专辑实体 — 对应 Spotify Album
/// </summary>
public class Album : IEntity
{
    public Guid Id { get; set; }
    public string SpotifyAlbumId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ReleaseDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? AlbumType { get; set; }
    public int TotalTracks { get; set; }

    public ICollection<Track> Tracks { get; set; } = new List<Track>();
}
