using Mapster;
using MediatR;
using MusicRec.Identity.Commands;
using MusicRec.Identity.DTOs;
using MusicRec.Identity.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Identity.Handlers;

/// <summary>
/// 更新用户资料处理器
/// </summary>
public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly MusicRecDbContext _db;

    public UpdateProfileCommandHandler(MusicRecDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _db.Set<User>().FindAsync([request.UserId], ct);
        if (user is null)
            throw new NotFoundException("用户不存在");

        user.Nickname = request.Nickname;
        user.AvatarUrl = request.AvatarUrl;
        await _db.SaveChangesAsync(ct);

        return user.Adapt<UserProfileDto>();
    }
}
