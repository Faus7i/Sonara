using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicRec.Catalog.Entities;
using MusicRec.Discovery.DTOs;
using MusicRec.Discovery.Queries;
using MusicRec.Discovery.Services;
using MusicRec.Infrastructure;
using MusicRec.Shared;
using MusicRec.Shared.Caching;

namespace MusicRec.Discovery.Handlers;

/// <summary>
/// 冷启动推荐处理器 — 面向新用户或无行为数据的用户
/// </summary>
/// <remarks>
/// 无需用户画像即可工作，基于热度 + 流派多样性采样。
/// GET /api/discovery/cold-start 不强制认证（UserId 可选）。
///
/// 冷启动策略：
///   1. 从曲库中取热门曲目（按 Popularity 降序）
///   2. 通过 DiscoveryEngine.ColdStart 做流派多样性采样
///   3. 确保返回结果覆盖至少 5 个不同流派
/// </remarks>
public class GetColdStartQueryHandler : IRequestHandler<GetColdStartQuery, IReadOnlyList<DiscoveryResultDto>>
{
    private readonly MusicRecDbContext _db;
    private readonly ICacheService _cache;
    private readonly RecommendationOptions _options;

    public GetColdStartQueryHandler(MusicRecDbContext db, ICacheService cache,
        IOptions<RecommendationOptions> options)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<DiscoveryResultDto>> Handle(
        GetColdStartQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ColdStart(request.Limit);

        // forceRefresh 时跳过缓存，强制重新计算
        if (request.ForceRefresh)
        {
            return await ComputeColdStart(request, ct);
        }

        return await _cache.GetOrCreateAsync<IReadOnlyList<DiscoveryResultDto>>(cacheKey, async () =>
        {
            return await ComputeColdStart(request, ct);
        }, TimeSpan.FromMinutes(5), ct);
    }

    private async Task<IReadOnlyList<DiscoveryResultDto>> ComputeColdStart(
        GetColdStartQuery request, CancellationToken ct)
    {
        // 冷启动不依赖用户数据，request.UserId 仅保留供后续日志/分析使用
        // 加载热门候选曲目
        var candidates = await _db.Set<Track>()
            .Include(t => t.TrackArtists!).ThenInclude(ta => ta.Artist)
            .Include(t => t.TrackGenres!).ThenInclude(tg => tg.Genre)
            .Include(t => t.Album)
            .AsNoTracking()
            .OrderByDescending(t => t.Popularity)
            .Take(_options.CandidatePoolSize)
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return Array.Empty<DiscoveryResultDto>();

        // 冷启动：热度 + 流派多样性（DiscoveryEngine.ColdStart 内部已使用 Random.Shared 打乱流派顺序）
        var discoveries = DiscoveryEngine.ColdStart(candidates, request.Limit);

        return discoveries.Select(d =>
        {
            var track = d.Track;
            var artistsSummary = track.TrackArtists is { Count: > 0 }
                ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist?.Name ?? "未知"))
                : "未知艺术家";

            var genres = track.TrackGenres?
                .Select(tg => tg.Genre?.Name)
                .Where(g => g is not null)
                .Select(g => g!)
                .ToList() ?? new List<string>();

            return new DiscoveryResultDto(
                Id: track.Id,
                Name: track.Name,
                CoverImageUrl: track.CoverImageUrl,
                DurationMs: track.DurationMs,
                Popularity: track.Popularity,
                ArtistsSummary: artistsSummary,
                AlbumName: track.Album?.Name,
                DiscoveryReason: d.DiscoveryReason,
                Genres: genres
            );
        }).ToList();
    }
}
