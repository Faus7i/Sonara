using MusicRec.Abstractions;

namespace MusicRec.UserBehavior.Entities;

/// <summary>
/// 用户播放历史 — 记录每次播放的开始时间、实际播放时长与完成状态
/// </summary>
/// <remarks>
/// DurationPlayed 在播放过程中由前端周期性上报更新，Completed 在播放完成时标记。
/// Source 用于区分播放来源：search（搜索结果）、playlist（歌单）、
/// favorites（收藏）、discovery（探索推荐）、recommendation（个性化推荐）。
/// </remarks>
public class UserPlayHistory : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TrackId { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    /// <summary>实际播放时长（毫秒），0 表示刚开始播放</summary>
    public int DurationPlayed { get; set; }
    /// <summary>是否完整播放完毕（非跳过、非中途退出）</summary>
    public bool Completed { get; set; }
    /// <summary>播放来源：search / playlist / favorites / discovery / recommendation</summary>
    public string? Source { get; set; }
}
