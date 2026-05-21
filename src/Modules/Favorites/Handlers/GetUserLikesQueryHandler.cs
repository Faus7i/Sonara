using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.Entities;
using MusicRec.Favorites.DTOs;
using MusicRec.Favorites.Entities;
using MusicRec.Favorites.Queries;
using MusicRec.Infrastructure;

namespace MusicRec.Favorites.Handlers;

/// <summary>
/// 获取收藏列表处理器 — 两阶段查询：先取 Like 记录，再批量 JOIN 曲目详情
/// </summary>
/// <remarks>
/// 采用"先分页查 UserLikes，再按 TrackId 批量查 Tracks"的两阶段策略，
/// 而非单次大 JOIN 查询。原因：
/// 1. 分页发生在 UserLikes 表（数据量小），JOIN 仅在当前页（≤50 条）上执行
/// 2. 避免 EF Core 在分页 + JOIN + 排序组合下生成低效的 SQL
/// 3. 已删除的曲目静默跳过（TrackId 在 Tracks 表中不存在时 continue）
/// </remarks>
public class GetUserLikesQueryHandler : IRequestHandler<GetUserLikesQuery, GetUserLikesResult>
{
    private readonly MusicRecDbContext _db;

    public GetUserLikesQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<GetUserLikesResult> Handle(GetUserLikesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        // 阶段一：分页查询收藏记录（按收藏时间降序）
        var query = _db.Set<UserLike>()
            .Where(ul => ul.UserId == request.UserId)
            .OrderByDescending(ul => ul.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var likes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (likes.Count == 0)
            return new GetUserLikesResult(new List<FavoriteTrackDto>(), totalCount, page, pageSize);

        // 阶段二：批量获取当前页曲目的详情（单次查询替代 N 次查询）
        var trackIds = likes.Select(l => l.TrackId).Distinct().ToList();
        var tracks = await _db.Set<Track>()
            .Where(t => trackIds.Contains(t.Id))
            .Include(t => t.Album)
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .ToListAsync(ct);

        var trackMap = tracks.ToDictionary(t => t.Id);

        // 阶段三：组装 DTO（展平 Track + Artist + Album 信息）
        var items = new List<FavoriteTrackDto>();
        foreach (var like in likes)
        {
            if (!trackMap.TryGetValue(like.TrackId, out var track))
                continue; // 曲目已被删除，静默跳过该收藏记录

            var artistsSummary = track.TrackArtists?.Count > 0
                ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist.Name))
                : "未知艺术家";

            items.Add(new FavoriteTrackDto(
                LikeId: like.Id,
                TrackId: track.Id,
                SpotifyTrackId: track.SpotifyTrackId,
                Name: track.Name,
                CoverImageUrl: track.CoverImageUrl,
                DurationMs: track.DurationMs,
                ArtistsSummary: artistsSummary,
                AlbumName: track.Album?.Name,
                LikedAt: like.CreatedAt
            ));
        }

        return new GetUserLikesResult(items, totalCount, page, pageSize);
    }
}
