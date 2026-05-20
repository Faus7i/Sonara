using MediatR;
using MusicRec.Identity.DTOs;

namespace MusicRec.Identity.Commands;

/// <summary>
/// 用户注册命令
/// </summary>
public record RegisterUserCommand(string Email, string Password, string Nickname)
    : IRequest<AuthResultDto>;
