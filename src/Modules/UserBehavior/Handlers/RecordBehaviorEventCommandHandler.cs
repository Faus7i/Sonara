using MediatR;
using Microsoft.Extensions.Logging;
using MusicRec.Infrastructure;
using MusicRec.UserBehavior.Commands;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 记录通用行为事件 — 点击、停留、滚动、详情页浏览等非播放行为（非关键写入）
/// </summary>
/// <remarks>
/// 与播放行为分离：播放生命周期由 RecordPlay/FinishPlay/SkipPlay 管理，
/// 其他一切 UI 交互事件均通过此 Handler 统一采集，供 Phase 5 推荐算法使用。
/// DB 写入被 try-catch 包裹，失败静默（规则 12）。
/// </remarks>
public class RecordBehaviorEventCommandHandler : IRequestHandler<RecordBehaviorEventCommand>
{
    private readonly MusicRecDbContext _db;
    private readonly ILogger<RecordBehaviorEventCommandHandler> _logger;

    public RecordBehaviorEventCommandHandler(MusicRecDbContext db, ILogger<RecordBehaviorEventCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(RecordBehaviorEventCommand request, CancellationToken ct)
    {
        var evt = new UserBehaviorEvent
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TrackId = request.TrackId,
            EventType = request.EventType,
            Context = request.Context,
            Duration = request.Duration,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow
        };
        _db.Set<UserBehaviorEvent>().Add(evt);

        try { await _db.SaveChangesAsync(ct); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "行为事件写入失败（已忽略）：UserId={UserId}, EventType={EventType}",
                request.UserId, request.EventType);
        }
    }
}
