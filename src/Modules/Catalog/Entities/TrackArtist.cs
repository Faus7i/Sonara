namespace MusicRec.Catalog.Entities;

/// <summary>
/// 曲目-艺术家关联表（多对多）
/// </summary>
public class TrackArtist
{
    public Guid TrackId { get; set; }
    public Guid ArtistId { get; set; }

    public Track Track { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
}
