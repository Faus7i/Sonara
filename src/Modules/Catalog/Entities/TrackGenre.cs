namespace MusicRec.Catalog.Entities;

/// <summary>
/// 曲目-流派关联表（多对多）
/// </summary>
public class TrackGenre
{
    public Guid TrackId { get; set; }
    public Guid GenreId { get; set; }

    public Track Track { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
