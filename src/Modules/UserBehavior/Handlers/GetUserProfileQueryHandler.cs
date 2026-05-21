using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Infrastructure;
using MusicRec.UserBehavior.DTOs;
using MusicRec.UserBehavior.Entities;
using MusicRec.UserBehavior.Queries;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 获取用户偏好画像 — 从 UserProfiles 表读取，不存在返回默认空画像
/// </summary>
/// <remarks>
/// 不自动触发刷新（避免每次查询都重算），前端需调用 POST /profile/refresh 手动刷新。
/// 默认空画像所有数值字段为 0，LastUpdatedAt 为 DateTime.MinValue（前端据此判断"未初始化"）。
/// </remarks>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly MusicRecDbContext _db;

    public GetUserProfileQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var profile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (profile is null)
        {
            return new UserProfileDto(
                UserId: request.UserId,
                FavoriteGenres: null,
                AvgEnergy: 0,
                AvgDanceability: 0,
                AvgValence: 0,
                AvgTempo: 0,
                AvgAcousticness: 0,
                TopArtists: null,
                TopTracks: null,
                ExplorationLevel: 0,
                TotalPlayCount: 0,
                LastUpdatedAt: DateTime.MinValue
            );
        }

        return profile.Adapt<UserProfileDto>();
    }
}
