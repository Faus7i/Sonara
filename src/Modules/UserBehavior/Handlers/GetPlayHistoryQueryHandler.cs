using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.Entities;
using MusicRec.Infrastructure;
using MusicRec.UserBehavior.DTOs;
using MusicRec.UserBehavior.Entities;
using MusicRec.UserBehavior.Queries;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 获取播放历史 — 三阶段分页 + 跨模块只读 JOIN
/// </summary>
/// <remarks>
/// 阶段一：在 UserPlayHistory 表完成分页（COUNT + SKIP/TAKE），
///         避免对全量数据做 JOIN 后再分页的 SQL 低效问题。
/// 阶段二：用当前页的 TrackId 集合批量加载曲目详情（跨模块读取 Catalog.Track）。
/// 阶段三：内存中组装 DTO，空值传播保护已删除曲目场景。
/// </remarks>
public class GetPlayHistoryQueryHandler : IRequestHandler<GetPlayHistoryQuery, PlayHistoryResult>
{
    private readonly MusicRecDbContext _db;

    public GetPlayHistoryQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<PlayHistoryResult> Handle(GetPlayHistoryQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        // ── 阶段一：播放记录分页（按时间降序） ──
        var query = _db.Set<UserPlayHistory>()
            .Where(h => h.UserId == request.UserId)
            .OrderByDescending(h => h.PlayedAt);

        var totalCount = await query.CountAsync(ct);
        var histories = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (histories.Count == 0)
            return new PlayHistoryResult(new List<PlayHistoryDto>(), totalCount, page, pageSize);

        // ── 阶段二：批量加载当前页曲目详情（跨模块只读访问 Catalog） ──
        var trackIds = histories.Select(h => h.TrackId).Distinct().ToList();
        var tracks = await _db.Set<Track>()
            .Where(t => trackIds.Contains(t.Id))
            .Include(t => t.Album)
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .ToListAsync(ct);

        var trackById = tracks.ToDictionary(t => t.Id);

        // ── 阶段三：内存组装 DTO（空值安全，处理已删除曲目） ──
        var items = histories.Select(h =>
        {
            trackById.TryGetValue(h.TrackId, out var track);

            // Spotify API 对本地文件/特殊曲目可能返回 null artists，使用 ?. 防护
            var artistsSummary = track?.TrackArtists?.Count > 0
                ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist.Name))
                : "未知艺术家";

            return new PlayHistoryDto(
                Id: h.Id,
                TrackId: h.TrackId,
                SpotifyTrackId: track?.SpotifyTrackId ?? "",
                TrackName: track?.Name ?? "已删除曲目",
                CoverImageUrl: track?.CoverImageUrl,
                DurationMs: track?.DurationMs ?? 0,
                ArtistsSummary: artistsSummary,
                AlbumName: track?.Album?.Name,
                PlayedAt: h.PlayedAt,
                DurationPlayed: h.DurationPlayed,
                Completed: h.Completed,
                Source: h.Source
            );
        }).ToList();

        return new PlayHistoryResult(items, totalCount, page, pageSize);
    }
}
