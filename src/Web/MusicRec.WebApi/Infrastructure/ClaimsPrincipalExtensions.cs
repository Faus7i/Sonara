using System.Security.Claims;
using MusicRec.Shared;

namespace MusicRec.WebApi.Infrastructure;

/// <summary>
/// JWT Claims 辅助扩展方法 — 从 ClaimsPrincipal 安全提取 UserId
/// </summary>
/// <remarks>
/// 替代各 Controller 中重复的 GetUserId() 私有方法。
/// 使用 Guid.TryParse 而非 Guid.Parse——当 Token 中的 sub claim 格式异常时
/// 抛出统一的 UnauthorizedException（而非未格式化的 FormatException）。
/// </remarks>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 获取当前登录用户的 Id — 用于 [Authorize] 端点
    /// </summary>
    /// <exception cref="UnauthorizedException">Token 缺失 sub claim 或格式无效</exception>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("Token 无效或已过期");
        return userId;
    }

    /// <summary>
    /// 获取当前登录用户的 Id — 用于可选认证端点（未登录返回 null）
    /// </summary>
    public static Guid? GetUserIdOrNull(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is not null && Guid.TryParse(sub, out var userId))
            return userId;
        return null;
    }
}
