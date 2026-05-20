using MusicRec.Abstractions;

namespace MusicRec.Catalog.Entities;

/// <summary>
/// 艺术家实体 — 对应 Spotify Artist
/// </summary>
public class Artist : IEntity
{
    public Guid Id { get; set; }
    public string SpotifyArtistId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // 逗号分隔字符串（如 "electronic,dance,house"）而非多对多关联表：
    // Artist 的 genre 通常只有 1-5 个，数量少且主要用途是展示，独立表得不偿失
    public string? Genres { get; set; }
    public string? ImageUrl { get; set; }
    public int Popularity { get; set; }

    public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
}
