using MediatR;
using MusicRec.Identity.DTOs;

namespace MusicRec.Identity.Commands;

/// <summary>
/// 更新用户资料命令
/// </summary>
public record UpdateProfileCommand(Guid UserId, string Nickname, string? AvatarUrl)
    : IRequest<UserProfileDto>;
