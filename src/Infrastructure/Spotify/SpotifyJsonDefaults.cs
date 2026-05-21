using System.Text.Json;

namespace MusicRec.Spotify;

/// <summary>
/// Spotify API 的 JSON 序列化默认配置 — 全局共享单例
/// </summary>
/// <remarks>
/// 使用 SnakeCaseLower 匹配 Spotify 的 snake_case 命名（如 duration_ms、track_number）。
/// PropertyNameCaseInsensitive 确保反序列化时忽略大小写差异。
/// 仿照 WebApi 层 JsonDefaults 的命名风格，表明这是静态配置而非 IOptions<T> 绑定。
/// </remarks>
public static class SpotifyJsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
