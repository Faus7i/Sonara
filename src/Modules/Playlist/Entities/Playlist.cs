using MusicRec.Abstractions;

namespace MusicRec.Playlists.Entities;

/// <summary>
/// 歌单实体 — 用户创建的音乐集合
/// </summary>
/// <remarks>
/// 删除歌单时通过 EF Cascade 自动删除关联的 PlaylistTracks（见 PlaylistConfiguration）。
/// CoverImageUrl 暂不自动填充，前端可根据首支曲目封面或默认图片渲染。
/// </remarks>
public class Playlist : IEntity
{
    public Guid Id { get; set; }
    /// <summary>歌单所有者用户 Id</summary>
    public Guid UserId { get; set; }
    /// <summary>歌单名称（最长 200 字符）</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>歌单描述（可选，最长 1000 字符）</summary>
    public string? Description { get; set; }
    /// <summary>封面图 URL（可选）</summary>
    public string? CoverImageUrl { get; set; }
    /// <summary>是否公开（false 时仅所有者可查看）</summary>
    public bool IsPublic { get; set; }
    /// <summary>创建时间（UTC）</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>最后更新时间（UTC）——任何曲目增删/排序/编辑均更新此字段</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>歌单中的曲目列表（按 OrderIndex 排序）</summary>
    public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
}
