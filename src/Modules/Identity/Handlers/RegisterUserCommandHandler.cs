using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Abstractions;
using MusicRec.Contracts.Events;
using MusicRec.Identity.Commands;
using MusicRec.Identity.DTOs;
using MusicRec.Identity.Entities;
using MusicRec.Identity.Services;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Identity.Handlers;

/// <summary>
/// 用户注册处理器 — 校验邮箱唯一性、哈希密码、持久化用户、签发 JWT 并发布跨模块事件
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    private readonly MusicRecDbContext _db;
    private readonly JwtService _jwtService;
    private readonly IPublisher _publisher;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
        MusicRecDbContext db,
        JwtService jwtService,
        IPublisher publisher,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwtService = jwtService;
        _publisher = publisher;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        // 邮箱唯一性校验 — 利用数据库唯一索引作为最后防线，此处做友好提示
        var existing = await _db.Set<User>()
            .AnyAsync(u => u.Email == request.Email, ct);

        if (existing)
            throw new ConflictException("该邮箱已被注册");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Nickname = request.Nickname,
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<User>().Add(user);
        // 并发注册同一邮箱时，唯一索引会触发 DbUpdateException，转换为 ConflictException
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627)) // 唯一约束冲突
        {
            throw new ConflictException("该邮箱已被注册");
        }

        // 发布 UserRegisteredEvent，后续模块（Recommendation、UserBehavior）通过订阅此事件
        // 自动初始化用户画像与行为数据，无需显式调用
        await _publisher.Publish(new UserRegisteredEvent(user.Id, user.Email, user.Nickname), ct);

        var (token, expiresAt) = _jwtService.GenerateToken(user.Id, user.Email);
        var profile = user.Adapt<UserProfileDto>();

        return new AuthResultDto(token, expiresAt, profile);
    }
}
