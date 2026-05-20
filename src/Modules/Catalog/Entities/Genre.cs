using MusicRec.Abstractions;

namespace MusicRec.Catalog.Entities;

/// <summary>
/// 流派实体
/// </summary>
public class Genre : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<TrackGenre> TrackGenres { get; set; } = new List<TrackGenre>();
}
