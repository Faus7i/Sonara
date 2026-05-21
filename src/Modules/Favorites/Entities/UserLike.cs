using MusicRec.Abstractions;

namespace MusicRec.Favorites.Entities;

/// <summary>
/// 用户收藏记录 — 用户与曲目的多对多收藏关联
/// </summary>
/// <remarks>
/// TrackId 指向 Catalog.Tracks 表的本地 Id（非 SpotifyTrackId）。
/// 数据库层以 (UserId, TrackId) 唯一索引保证同一用户不会重复收藏同一曲目。
/// 不设置导航属性——跨模块实体关联通过 Handler 中的 JOIN 查询实现。
/// </remarks>
public class UserLike : IEntity
{
    public Guid Id { get; set; }
    /// <summary>收藏者用户 Id</summary>
    public Guid UserId { get; set; }
    /// <summary>被收藏曲目的本地 Id（对应 Tracks.Id）</summary>
    public Guid TrackId { get; set; }
    /// <summary>收藏时间（UTC）</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
