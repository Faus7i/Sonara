using Mapster;
using MediatR;
using MusicRec.Identity.DTOs;
using MusicRec.Identity.Entities;
using MusicRec.Identity.Queries;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Identity.Handlers;

/// <summary>
/// 获取用户资料处理器
/// </summary>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly MusicRecDbContext _db;

    public GetUserProfileQueryHandler(MusicRecDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var user = await _db.Set<User>().FindAsync([request.UserId], ct);
        if (user is null)
            throw new NotFoundException("用户不存在");

        return user.Adapt<UserProfileDto>();
    }
}
