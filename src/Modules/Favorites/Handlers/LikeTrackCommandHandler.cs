using Mapster;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.Entities;
using MusicRec.Favorites.Commands;
using MusicRec.Favorites.DTOs;
using MusicRec.Favorites.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Favorites.Handlers;

/// <summary>
/// 收藏曲目处理器 — 幂等设计，重复收藏不报错
/// </summary>
/// <remarks>
/// 并发安全：数据库唯一索引 (UserId, TrackId) 是最后防线。
/// 当两个并发请求同时通过"未收藏"检查时，第一个成功插入而第二个触发
/// SQL 错误 2601/2627（唯一约束冲突），由 try-catch 捕获后重新查询返回已有记录，
/// 确保客户端始终得到正确响应而非 500 错误。
/// </remarks>
public class LikeTrackCommandHandler : IRequestHandler<LikeTrackCommand, UserLikeDto>
{
    private readonly MusicRecDbContext _db;

    public LikeTrackCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task<UserLikeDto> Handle(LikeTrackCommand request, CancellationToken ct)
    {
        // 第一步：幂等检查——已收藏则直接返回已有记录
        var existing = await _db.Set<UserLike>()
            .FirstOrDefaultAsync(ul => ul.UserId == request.UserId && ul.TrackId == request.TrackId, ct);
        if (existing is not null)
            return existing.Adapt<UserLikeDto>();

        // 第二步：验证曲目在本地库中存在（防止收藏未导入的曲目）
        var trackExists = await _db.Set<Track>().AnyAsync(t => t.Id == request.TrackId, ct);
        if (!trackExists)
            throw new NotFoundException("曲目不存在，请先从 Spotify 导入该曲目");

        var like = new UserLike
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TrackId = request.TrackId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Set<UserLike>().Add(like);

        // 第三步：写入——捕获并发唯一约束冲突（SQL 2601/2627）
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            // 并发窗口：另一请求已插入相同 (UserId, TrackId)，重新查询返回
            var concurrent = await _db.Set<UserLike>()
                .FirstOrDefaultAsync(ul => ul.UserId == request.UserId && ul.TrackId == request.TrackId, ct);
            return concurrent!.Adapt<UserLikeDto>();
        }

        return like.Adapt<UserLikeDto>();
    }
}
