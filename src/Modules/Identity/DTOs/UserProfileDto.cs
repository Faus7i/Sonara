namespace MusicRec.Identity.DTOs;

/// <summary>
/// 用户资料响应
/// </summary>
public record UserProfileDto(
    Guid Id,
    string Email,
    string Nickname,
    string? AvatarUrl,
    DateTime CreatedAt);

/// <summary>
/// 更新用户资料请求
/// </summary>
public record UpdateProfileRequest(string Nickname, string? AvatarUrl);
