namespace MusicRec.Spotify;

/// <summary>
/// Spotify API 配置 — 从 appsettings.json 的 "Spotify" 节点绑定
/// </summary>
public class SpotifyOptions
{
    public const string SectionName = "Spotify";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
