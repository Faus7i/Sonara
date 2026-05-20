using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Abstractions;
using MusicRec.Identity.Commands;
using MusicRec.Identity.DTOs;
using MusicRec.Identity.Entities;
using MusicRec.Identity.Services;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Identity.Handlers;

/// <summary>
/// 用户登录处理器 — 查询用户后使用恒定时间比较验证密码（防止时序攻击）
/// </summary>
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResultDto>
{
    private readonly MusicRecDbContext _db;
    private readonly JwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginUserCommandHandler(
        MusicRecDbContext db,
        JwtService jwtService,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResultDto> Handle(LoginUserCommand request, CancellationToken ct)
    {
        var user = await _db.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        // 无论用户是否存在都执行哈希验证，防止通过响应时间枚举已注册邮箱
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("邮箱或密码错误");

        var (token, expiresAt) = _jwtService.GenerateToken(user.Id, user.Email);
        var profile = user.Adapt<UserProfileDto>();

        return new AuthResultDto(token, expiresAt, profile);
    }
}
