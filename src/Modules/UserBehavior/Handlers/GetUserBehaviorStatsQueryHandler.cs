using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Infrastructure;
using MusicRec.UserBehavior.DTOs;
using MusicRec.UserBehavior.Entities;
using MusicRec.UserBehavior.Queries;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 获取用户行为统计摘要 — 播放/跳过趋势、偏好时段、来源分布
/// </summary>
/// <remarks>
/// 多个 CountAsync 调用各自生成一条 SQL，对于毕业设计规模的数据量（&lt;10 万条/用户）
/// 这种写法比单次复杂 GroupBy 更易维护。若未来数据量增长，可合并为一次聚合查询。
/// 跳过判定：DurationPlayed > 0 且未完成（中途暂停也算跳过）。
/// </remarks>
public class GetUserBehaviorStatsQueryHandler : IRequestHandler<GetUserBehaviorStatsQuery, UserBehaviorStatsDto>
{
    private readonly MusicRecDbContext _db;

    public GetUserBehaviorStatsQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<UserBehaviorStatsDto> Handle(GetUserBehaviorStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        var playHistory = _db.Set<UserPlayHistory>().Where(h => h.UserId == request.UserId);

        // 基础计数
        var totalPlayCount = await playHistory.CountAsync(ct);
        var totalSkipCount = await playHistory.CountAsync(
            h => !h.Completed && h.DurationPlayed > 0, ct);
        var totalCompleteCount = await playHistory.CountAsync(h => h.Completed, ct);
        var skipRate = totalPlayCount > 0 ? (double)totalSkipCount / totalPlayCount : 0;

        // 近期活跃度
        var last7DaysPlayCount = await playHistory.CountAsync(h => h.PlayedAt >= last7Days, ct);
        var last30DaysPlayCount = await playHistory.CountAsync(h => h.PlayedAt >= last30Days, ct);

        // 偏好时段 — 按小时分组，取播放最多的时段
        var favoriteHour = 0;
        if (totalPlayCount > 0)
        {
            var hourGroups = await playHistory
                .GroupBy(h => h.PlayedAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefaultAsync(ct);
            favoriteHour = hourGroups?.Hour ?? 0;
        }

        // 播放来源偏好 — 统计 source 字段分布，取最多的来源
        var topSource = await playHistory
            .Where(h => h.Source != null)
            .GroupBy(h => h.Source!)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Select(g => g.Source)
            .FirstOrDefaultAsync(ct);

        return new UserBehaviorStatsDto(
            TotalPlayCount: totalPlayCount,
            TotalSkipCount: totalSkipCount,
            TotalCompleteCount: totalCompleteCount,
            SkipRate: Math.Round(skipRate, 4),
            Last7DaysPlayCount: last7DaysPlayCount,
            Last30DaysPlayCount: last30DaysPlayCount,
            FavoriteHour: favoriteHour,
            TopSource: topSource
        );
    }
}
