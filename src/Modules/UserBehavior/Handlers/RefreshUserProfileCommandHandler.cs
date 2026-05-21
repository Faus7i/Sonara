using System.Text.Json;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Catalog.Entities;
using MusicRec.Contracts.Events;
using MusicRec.Infrastructure;
using MusicRec.UserBehavior.Commands;
using MusicRec.UserBehavior.DTOs;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Handlers;

/// <summary>
/// 刷新用户画像 — 聚合播放历史 + 收藏数据，计算加权音频偏好与流派分布
/// </summary>
/// <remarks>
/// 算法分四阶段：
///   1. 采集：播放统计 + 收藏曲目 ID
///   2. 特征提取：关联曲目 → AudioFeatures，按播放次数加权平均
///   3. 偏好分析：跨模块读取 Artist.Genres 统计流派 + Top 艺术家/曲目
///   4. 持久化：Upsert UserProfile + 发布 UserBehaviorUpdatedEvent 通知 Phase 5
/// </remarks>
public class RefreshUserProfileCommandHandler : IRequestHandler<RefreshUserProfileCommand, UserProfileDto>
{
    private readonly MusicRecDbContext _db;
    private readonly IPublisher _publisher;

    public RefreshUserProfileCommandHandler(MusicRecDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<UserProfileDto> Handle(RefreshUserProfileCommand request, CancellationToken ct)
    {
        // ═══════════════════════════════════════════════════════════════
        // 阶段一：采集播放统计与收藏数据
        // ═══════════════════════════════════════════════════════════════

        // 按 TrackId 分组统计播放次数
        var playStats = await _db.Set<UserPlayHistory>()
            .Where(h => h.UserId == request.UserId)
            .GroupBy(h => h.TrackId)
            .Select(g => new { TrackId = g.Key, PlayCount = g.Count() })
            .ToListAsync(ct);

        var totalPlayCount = playStats.Sum(p => p.PlayCount);
        var playedTrackIds = playStats.Select(p => p.TrackId).Distinct().ToList();

        // 获取收藏曲目 ID（跨模块只读访问 Favorites 模块）
        var likedTrackIds = await _db.Set<MusicRec.Favorites.Entities.UserLike>()
            .Where(ul => ul.UserId == request.UserId)
            .Select(ul => ul.TrackId)
            .Distinct()
            .ToListAsync(ct);

        // 合并播放 + 收藏的所有曲目 ID，一次性查询曲目详情（避免两次 DB 往返）
        var allRelevantTrackIds = playedTrackIds.Union(likedTrackIds).Distinct().ToList();

        // ═══════════════════════════════════════════════════════════════
        // 阶段二：批量加载曲目详情 + 音频特征
        // ═══════════════════════════════════════════════════════════════

        var allTracks = allRelevantTrackIds.Count > 0
            ? await _db.Set<Track>()
                .Where(t => allRelevantTrackIds.Contains(t.Id))
                .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
                .ToListAsync(ct)
            : new List<Track>();

        // 构建快速查找字典
        var trackById = allTracks.ToDictionary(t => t.Id);

        // 仅对播放过的曲目查询音频特征（收藏但未播放的曲目不参与特征加权）
        var playedSpotifyIds = playedTrackIds
            .Select(id => trackById.TryGetValue(id, out var t) ? t.SpotifyTrackId : null)
            .Where(id => id is not null)
            .Cast<string>()
            .Distinct()
            .ToList();

        var audioFeatures = playedSpotifyIds.Count > 0
            ? await _db.Set<TrackAudioFeature>()
                .Where(af => playedSpotifyIds.Contains(af.SpotifyTrackId))
                .ToListAsync(ct)
            : new List<TrackAudioFeature>();

        var audioBySpotifyId = audioFeatures.ToDictionary(af => af.SpotifyTrackId);

        // ═══════════════════════════════════════════════════════════════
        // 阶段三：加权平均音频特征（按播放次数加权）
        // ═══════════════════════════════════════════════════════════════

        double totalWeight = 0;
        double sumEnergy = 0, sumDanceability = 0, sumValence = 0;
        double sumTempo = 0, sumAcousticness = 0;

        foreach (var stat in playStats)
        {
            // 跳过本地库中已不存在的曲目
            if (!trackById.TryGetValue(stat.TrackId, out var track))
                continue;

            // 跳过无音频特征的曲目（如本地文件、未导入 AudioFeatures 的曲目）
            if (!audioBySpotifyId.TryGetValue(track.SpotifyTrackId, out var af))
                continue;

            var weight = stat.PlayCount;
            sumEnergy += af.Energy * weight;
            sumDanceability += af.Danceability * weight;
            sumValence += af.Valence * weight;
            sumTempo += af.Tempo * weight;
            sumAcousticness += af.Acousticness * weight;
            totalWeight += weight;
        }

        var avgEnergy = totalWeight > 0 ? sumEnergy / totalWeight : 0;
        var avgDanceability = totalWeight > 0 ? sumDanceability / totalWeight : 0;
        var avgValence = totalWeight > 0 ? sumValence / totalWeight : 0;
        var avgTempo = totalWeight > 0 ? sumTempo / totalWeight : 0;
        var avgAcousticness = totalWeight > 0 ? sumAcousticness / totalWeight : 0;

        // ═══════════════════════════════════════════════════════════════
        // 阶段四：偏好分析 — 流派分布、Top 艺术家、Top 曲目
        // ═══════════════════════════════════════════════════════════════

        // 4.1 统计流派分布（从 Artist.Genres 逗号分隔字段解析）
        var genreCounts = new Dictionary<string, int>();
        foreach (var t in allTracks)
        {
            foreach (var ta in t.TrackArtists)
            {
                if (string.IsNullOrEmpty(ta.Artist.Genres)) continue;
                foreach (var genre in ta.Artist.Genres.Split(','))
                {
                    var trimmed = genre.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    genreCounts.TryGetValue(trimmed, out var count);
                    genreCounts[trimmed] = count + 1;
                }
            }
        }

        var favoriteGenres = genreCounts.Count > 0
            ? JsonSerializer.Serialize(genreCounts.OrderByDescending(kv => kv.Value).Take(5).Select(kv => kv.Key))
            : null;

        // 4.2 统计艺术家播放次数（播放 + 收藏加权）
        var artistPlayCounts = new Dictionary<(Guid Id, string Name), int>();
        foreach (var t in allTracks)
        {
            var playCount = playStats.FirstOrDefault(p => p.TrackId == t.Id)?.PlayCount ?? 1;
            foreach (var ta in t.TrackArtists)
            {
                var key = (ta.Artist.Id, ta.Artist.Name);
                artistPlayCounts.TryGetValue(key, out var count);
                artistPlayCounts[key] = count + playCount;
            }
        }

        var topArtists = artistPlayCounts.Count > 0
            ? JsonSerializer.Serialize(artistPlayCounts.OrderByDescending(kv => kv.Value).Take(10)
                .Select(kv => new { id = kv.Key.Id, name = kv.Key.Name, count = kv.Value }))
            : null;

        // 4.3 Top 曲目（按播放次数降序取前 10）
        var topTracks = playStats.Count > 0
            ? JsonSerializer.Serialize(playStats.OrderByDescending(p => p.PlayCount).Take(10)
                .Select(p =>
                {
                    trackById.TryGetValue(p.TrackId, out var t);
                    return new { id = p.TrackId, name = t?.Name ?? "未知", count = p.PlayCount };
                }))
            : null;

        // 4.4 探索度 = 不同流派数 / 关联曲目数（0~1，越高代表品味越广泛）
        var explorationLevel = allRelevantTrackIds.Count > 0
            ? Math.Min(1.0, (double)genreCounts.Count / Math.Max(1, allRelevantTrackIds.Count))
            : 0;

        // ═══════════════════════════════════════════════════════════════
        // 阶段五：Upsert 用户画像 + 发布领域事件
        // ═══════════════════════════════════════════════════════════════

        var profile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (profile is null)
        {
            profile = new UserProfile { UserId = request.UserId };
            _db.Set<UserProfile>().Add(profile);
        }

        profile.FavoriteGenres = favoriteGenres;
        profile.AvgEnergy = avgEnergy;
        profile.AvgDanceability = avgDanceability;
        profile.AvgValence = avgValence;
        profile.AvgTempo = avgTempo;
        profile.AvgAcousticness = avgAcousticness;
        profile.TopArtists = topArtists;
        profile.TopTracks = topTracks;
        profile.ExplorationLevel = explorationLevel;
        profile.TotalPlayCount = totalPlayCount;
        profile.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // 通知 Phase 5 的 Recommendation 模块：用户画像已更新，可刷新推荐列表
        await _publisher.Publish(new UserBehaviorUpdatedEvent(request.UserId), ct);

        return profile.Adapt<UserProfileDto>();
    }
}
