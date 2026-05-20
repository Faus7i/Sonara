namespace MusicRec.Identity.DTOs;

/// <summary>
/// 登录/注册成功后返回的认证结果
/// </summary>
public record AuthResultDto(
    string AccessToken,
    DateTime ExpiresAt,
    UserProfileDto User);
