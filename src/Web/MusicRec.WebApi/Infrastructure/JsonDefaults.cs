using System.Text.Json;

namespace MusicRec.WebApi.Infrastructure;

/// <summary>
/// 全局 JSON 序列化配置 — 确保中间件与 JWT 事件输出一致的 camelCase 格式
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
