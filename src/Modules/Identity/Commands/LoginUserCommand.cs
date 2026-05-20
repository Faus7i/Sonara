using MediatR;
using MusicRec.Identity.DTOs;

namespace MusicRec.Identity.Commands;

/// <summary>
/// 用户登录命令
/// </summary>
public record LoginUserCommand(string Email, string Password)
    : IRequest<AuthResultDto>;
