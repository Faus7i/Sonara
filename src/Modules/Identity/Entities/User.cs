using MusicRec.Abstractions;

namespace MusicRec.Identity.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User : IEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
