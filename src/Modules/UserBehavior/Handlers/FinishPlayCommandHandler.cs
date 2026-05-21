using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicRec.Infrastructure;
using MusicRec.Shared;
using MusicRec.UserBehavior.Commands;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 记录播放完成 — 更新播放时长与完成状态（幂等，所有权校验）
/// </summary>
/// <remarks>
/// 幂等规则：已标记 Completed 的记录重复调用静默返回。
/// 所有权校验：仅播放记录所有者可标记完成，非所有者抛 UnauthorizedException。
/// 非关键写入：DB 写入失败记录日志不抛异常（规则 12）。
/// </remarks>
public class FinishPlayCommandHandler : IRequestHandler<FinishPlayCommand>
{
    private readonly MusicRecDbContext _db;
    private readonly ILogger<FinishPlayCommandHandler> _logger;

    public FinishPlayCommandHandler(MusicRecDbContext db, ILogger<FinishPlayCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(FinishPlayCommand request, CancellationToken ct)
    {
        var history = await _db.Set<UserPlayHistory>()
            .FirstOrDefaultAsync(h => h.Id == request.PlayHistoryId, ct);

        if (history is null)
        {
            _logger.LogWarning("播放完成记录失败：播放记录 {Id} 不存在", request.PlayHistoryId);
            return;
        }

        if (history.UserId != request.UserId)
            throw new UnauthorizedException("无权操作他人的播放记录");

        // 幂等：已标记完成则静默返回
        if (history.Completed)
            return;

        history.DurationPlayed = request.DurationPlayed;
        history.Completed = true;

        try { await _db.SaveChangesAsync(ct); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "播放完成记录写入失败（已忽略）：PlayHistoryId={Id}", request.PlayHistoryId);
        }
    }
}
