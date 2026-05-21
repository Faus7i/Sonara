using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Favorites.Entities;
using MusicRec.Favorites.Queries;
using MusicRec.Infrastructure;

namespace MusicRec.Favorites.Handlers;

/// <summary>
/// 检查收藏状态处理器 — 供前端判断红心图标是否点亮
/// </summary>
public class CheckLikeQueryHandler : IRequestHandler<CheckLikeQuery, bool>
{
    private readonly MusicRecDbContext _db;

    public CheckLikeQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<bool> Handle(CheckLikeQuery request, CancellationToken ct)
    {
        return await _db.Set<UserLike>()
            .AnyAsync(ul => ul.UserId == request.UserId && ul.TrackId == request.TrackId, ct);
    }
}
