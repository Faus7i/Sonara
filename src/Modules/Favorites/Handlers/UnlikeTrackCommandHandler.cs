using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Favorites.Commands;
using MusicRec.Favorites.Entities;
using MusicRec.Infrastructure;

namespace MusicRec.Favorites.Handlers;

/// <summary>
/// 取消收藏处理器 — 幂等设计，对不存在/已取消的记录静默返回成功
/// </summary>
public class UnlikeTrackCommandHandler : IRequestHandler<UnlikeTrackCommand>
{
    private readonly MusicRecDbContext _db;

    public UnlikeTrackCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task Handle(UnlikeTrackCommand request, CancellationToken ct)
    {
        var like = await _db.Set<UserLike>()
            .FirstOrDefaultAsync(ul => ul.UserId == request.UserId && ul.TrackId == request.TrackId, ct);

        if (like is null)
            return; // 幂等：记录不存在或已取消，静默返回成功

        _db.Set<UserLike>().Remove(like);
        await _db.SaveChangesAsync(ct);
    }
}
