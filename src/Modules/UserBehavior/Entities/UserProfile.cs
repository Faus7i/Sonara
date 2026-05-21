namespace MusicRec.UserBehavior.Entities;

/// <summary>
/// 用户偏好画像 — 基于播放/收藏行为聚合计算的音乐品味摘要（一对一 User）
/// </summary>
/// <remarks>
/// UserId 即主键，与 Users 表一对一，不实现 IEntity（无需独立 Id）。
/// FavoriteGenres / TopArtists / TopTracks 以 JSON 数组字符串存储，便于灵活查询。
/// 画像由 RefreshUserProfileCommand 定期或事件驱动刷新。
/// </remarks>
public class UserProfile
{
    public Guid UserId { get; set; }
    /// <summary>偏好流派 Top 5（JSON 数组字符串）</summary>
    public string? FavoriteGenres { get; set; }
    /// <summary>加权平均能量（按播放次数）</summary>
    public double AvgEnergy { get; set; }
    /// <summary>加权平均舞蹈性</summary>
    public double AvgDanceability { get; set; }
    /// <summary>加权平均情绪效价</summary>
    public double AvgValence { get; set; }
    /// <summary>加权平均 BPM</summary>
    public double AvgTempo { get; set; }
    /// <summary>加权平均原声度</summary>
    public double AvgAcousticness { get; set; }
    /// <summary>Top 10 艺术家（JSON 数组，含 id/name/count）</summary>
    public string? TopArtists { get; set; }
    /// <summary>Top 10 曲目（JSON 数组，含 id/name/count）</summary>
    public string? TopTracks { get; set; }
    /// <summary>探索意愿 0~1（不同流派数 / 关联曲目数）</summary>
    public double ExplorationLevel { get; set; }
    /// <summary>总播放次数（用于推荐算法的活跃度权重）</summary>
    public int TotalPlayCount { get; set; }
    /// <summary>画像最后刷新时间</summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
