using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicRec.Infrastructure;
using MusicRec.Shared;
using MusicRec.UserBehavior.Commands;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 记录跳过行为 — 更新播放时长并写入跳过事件（幂等）
/// </summary>
/// <remarks>
/// 幂等规则：已完成播放的记录不标记为跳过（用户听完后拖回进度条不算跳过）；
/// 相同跳过位置重复调用静默返回（避免重复写入行为事件）。
/// </remarks>
public class SkipPlayCommandHandler : IRequestHandler<SkipPlayCommand>
{
    private readonly MusicRecDbContext _db;
    private readonly ILogger<SkipPlayCommandHandler> _logger;

    public SkipPlayCommandHandler(MusicRecDbContext db, ILogger<SkipPlayCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(SkipPlayCommand request, CancellationToken ct)
    {
        var history = await _db.Set<UserPlayHistory>()
            .FirstOrDefaultAsync(h => h.Id == request.PlayHistoryId, ct);

        if (history is null)
        {
            _logger.LogWarning("跳过记录失败：播放记录 {Id} 不存在", request.PlayHistoryId);
            return;
        }

        if (history.UserId != request.UserId)
            throw new UnauthorizedException("无权操作他人的播放记录");

        if (history.Completed)
            return; // 幂等：已完成播放的记录不标记为跳过

        if (history.DurationPlayed == request.SkippedAtPositionMs)
            return; // 幂等：已标记相同跳过位置则静默返回

        history.DurationPlayed = request.SkippedAtPositionMs;
        history.Completed = false;

        _db.Set<UserBehaviorEvent>().Add(new UserBehaviorEvent
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TrackId = history.TrackId,
            EventType = "skip",
            Context = "player",
            CreatedAt = DateTime.UtcNow
        });

        try { await _db.SaveChangesAsync(ct); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "跳过记录写入失败（已忽略）：PlayHistoryId={Id}", request.PlayHistoryId);
        }
    }
}
