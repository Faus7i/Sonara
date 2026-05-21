namespace MusicRec.Shared;

/// <summary>
/// 推荐算法可配置参数 — 绑定自 appsettings.json 的 "Recommendation" 节
/// </summary>
public class RecommendationOptions
{
    public const string SectionName = "Recommendation";

    /// <summary>候选曲目池大小（默认 200）</summary>
    public int CandidatePoolSize { get; set; } = 200;

    /// <summary>高分曲目比例（默认 0.70）</summary>
    public double ExploitRatio { get; set; } = 0.70;

    /// <summary>相邻流派曲目比例（默认 0.20）</summary>
    public double AdjacentRatio { get; set; } = 0.20;

    /// <summary>新风格探索比例（默认 0.10）</summary>
    public double NovelRatio { get; set; } = 0.10;
}
