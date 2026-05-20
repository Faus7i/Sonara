namespace MusicRec.Identity.DTOs;

/// <summary>
/// 注册请求
/// </summary>
public record RegisterRequest(string Email, string Password, string Nickname);

/// <summary>
/// 登录请求
/// </summary>
public record LoginRequest(string Email, string Password);
