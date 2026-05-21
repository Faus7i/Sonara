using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicRec.Catalog.Entities;
using MusicRec.Infrastructure;
using MusicRec.UserBehavior.Commands;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 记录播放开始 — 非关键写入（规则 12），失败记录日志不抛异常
/// 始终返回播放历史 ID，即使写入失败也返回（前端可用此 ID 调用 finish/skip，
/// 若写入失败则后续 finish/skip 的 Handler 会因记录不存在而静默返回）
/// </summary>
public class RecordPlayCommandHandler : IRequestHandler<RecordPlayCommand, Guid>
{
    private readonly MusicRecDbContext _db;
    private readonly ILogger<RecordPlayCommandHandler> _logger;

    public RecordPlayCommandHandler(MusicRecDbContext db, ILogger<RecordPlayCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> Handle(RecordPlayCommand request, CancellationToken ct)
    {
        var historyId = Guid.NewGuid();

        var trackExists = await _db.Set<Track>().AnyAsync(t => t.Id == request.TrackId, ct);
        if (!trackExists)
        {
            _logger.LogWarning("播放记录失败：曲目 {TrackId} 不在本地库中", request.TrackId);
            return historyId;
        }

        var history = new UserPlayHistory
        {
            Id = historyId,
            UserId = request.UserId,
            TrackId = request.TrackId,
            PlayedAt = DateTime.UtcNow,
            DurationPlayed = 0,
            Completed = false,
            Source = request.Source
        };
        _db.Set<UserPlayHistory>().Add(history);

        try { await _db.SaveChangesAsync(ct); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "播放记录写入失败（已忽略）：UserId={UserId}, TrackId={TrackId}",
                request.UserId, request.TrackId);
        }

        return historyId;
    }
}
