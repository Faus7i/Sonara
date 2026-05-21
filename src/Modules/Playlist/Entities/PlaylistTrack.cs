using MusicRec.Abstractions;

namespace MusicRec.Playlists.Entities;

/// <summary>
/// 歌单曲目关联实体 — 记录曲目在歌单中的顺序和加入时间
/// </summary>
/// <remarks>
/// (PlaylistId, TrackId) 有唯一索引——同一曲目不能在同一歌单中出现两次。
/// OrderIndex 从 0 开始递增，由 AddTrackToPlaylistCommandHandler 自动计算。
/// TrackId 指向 Catalog.Tracks 表的本地 Id（非 SpotifyTrackId）。
/// </remarks>
public class PlaylistTrack : IEntity
{
    public Guid Id { get; set; }
    /// <summary>所属歌单 Id</summary>
    public Guid PlaylistId { get; set; }
    /// <summary>曲目本地 Id（对应 Tracks.Id）</summary>
    public Guid TrackId { get; set; }
    /// <summary>在歌单中的排列顺序（0 起始）</summary>
    public int OrderIndex { get; set; }
    /// <summary>加入歌单的时间（UTC）</summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>导航属性——所属歌单</summary>
    public Playlist Playlist { get; set; } = null!;
}
