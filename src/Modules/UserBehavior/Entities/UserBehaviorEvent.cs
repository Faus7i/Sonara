using MusicRec.Abstractions;

namespace MusicRec.UserBehavior.Entities;

/// <summary>
/// 用户行为事件 — 细粒度记录点击、停留、滚动、跳过等非播放行为
/// </summary>
/// <remarks>
/// 与 UserPlayHistory 互补：播放生命周期（开始/完成/跳过）走 PlayHistory，
/// 其余 UI 交互（详情页停留时长、按钮点击、页面滚动深度）走此表。
/// EventType 取值：click / dwell / scroll / skip / view_detail
/// Context 取值：search_results / playlist_page / track_detail / discovery_feed
/// </remarks>
public class UserBehaviorEvent : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>关联曲目（非曲目相关事件可为 null）</summary>
    public Guid? TrackId { get; set; }
    /// <summary>事件类型：click / dwell / scroll / skip / view_detail</summary>
    public string EventType { get; set; } = string.Empty;
    /// <summary>事件发生的页面上下文</summary>
    public string? Context { get; set; }
    /// <summary>停留时长（毫秒），仅 dwell 类型事件使用</summary>
    public int? Duration { get; set; }
    /// <summary>扩展元数据 JSON，用于未来扩展</summary>
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
