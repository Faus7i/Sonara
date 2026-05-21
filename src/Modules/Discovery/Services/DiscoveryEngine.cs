using MusicRec.Catalog.Entities;

namespace MusicRec.Discovery.Services;

/// <summary>
/// 探索推荐引擎 — 提供冷启动与探索推荐的评分与采样逻辑
/// </summary>
/// <remarks>
/// 冷启动策略：基于流行度 + 流派多样性采样，确保返回结果覆盖多个风格。
/// 探索策略：70% 热门 + 20% 流派相关 + 10% 完全随机，适用于已有部分行为的用户。
/// </remarks>
public static class DiscoveryEngine
{
    /// <summary>
    /// 流派多样性采样 — 按流派轮询采样 + 偏好流派优先
    /// </summary>
    /// <remarks>
    /// 采样策略（三级降级）：
    ///   1. 偏好流派优先 — 从用户喜欢的流派中各取 1 首最热门曲目
    ///   2. 轮询采样 — 从所有流派轮流取 1 首，确保结果覆盖多个风格
    ///   3. 热度兜底 — 如仍不足，按 Spotify popularity 降序补足
    ///
    /// 轮询优于"每个流派取 N 首"：如果 20 首候选中有 15 首是 pop，按比例采会导致
    /// 结果被 pop 主导，失去多样性意义。轮询确保小众流派（如 ambient、blues）也有曝光机会。
    /// </remarks>
    /// <param name="candidates">候选曲目列表</param>
    /// <param name="limit">目标返回数量</param>
    /// <param name="preferredGenres">偏好流派（可选，优先采样这些流派）</param>
    public static List<DiscoveryTrack> SampleWithDiversity(
        List<Track> candidates, int limit, HashSet<string>? preferredGenres = null)
    {
        // 按流派分组（TryGetValue 减少字典查找次数）
        var genreGroups = new Dictionary<string, List<Track>>();
        foreach (var track in candidates)
        {
            var genres = track.TrackGenres?
                .Select(tg => tg.Genre?.Name)
                .Where(g => g is not null)
                .Select(g => g!)
                .ToList() ?? new List<string>();

            foreach (var genre in genres)
            {
                if (!genreGroups.TryGetValue(genre, out var list))
                {
                    list = new List<Track>();
                    genreGroups[genre] = list;
                }
                list.Add(track);
            }

            // 无流派曲目放入 "other" 组（确保多样性采样覆盖所有曲目）
            if (genres.Count == 0)
            {
                if (!genreGroups.TryGetValue("other", out var otherList))
                {
                    otherList = new List<Track>();
                    genreGroups["other"] = otherList;
                }
                otherList.Add(track);
            }
        }

        var result = new List<DiscoveryTrack>();
        var usedTrackIds = new HashSet<Guid>();

        // 优先从偏好流派采样
        if (preferredGenres is { Count: > 0 })
        {
            foreach (var genre in preferredGenres)
            {
                if (result.Count >= limit) break;
                if (!genreGroups.TryGetValue(genre, out var tracks)) continue;

                var pick = tracks.Where(t => !usedTrackIds.Contains(t.Id))
                    .OrderByDescending(t => t.Popularity)
                    .FirstOrDefault();
                if (pick is null) continue;

                usedTrackIds.Add(pick.Id);
                result.Add(new DiscoveryTrack(pick, $"流派探索 · {genre}"));
            }
        }

        // 从所有流派轮询采样
        var genreKeys = genreGroups.Keys.OrderBy(_ => Random.Shared.Next()).ToList();
        var roundRobinIndex = 0;
        while (result.Count < limit && genreKeys.Count > 0)
        {
            var genre = genreKeys[roundRobinIndex % genreKeys.Count];
            roundRobinIndex++;

            if (!genreGroups.TryGetValue(genre, out var tracks)) continue;
            var pick = tracks.Where(t => !usedTrackIds.Contains(t.Id))
                .OrderByDescending(t => t.Popularity)
                .FirstOrDefault();
            if (pick is null) continue;

            usedTrackIds.Add(pick.Id);
            var reason = genre == "other" ? "发现新声音" : $"流派探索 · {genre}";
            result.Add(new DiscoveryTrack(pick, reason));
        }

        // 如仍不足，按热度补充
        foreach (var track in candidates.OrderByDescending(t => t.Popularity))
        {
            if (result.Count >= limit) break;
            if (usedTrackIds.Contains(track.Id)) continue;
            result.Add(new DiscoveryTrack(track, "热门推荐"));
        }

        return result;
    }

    /// <summary>
    /// 冷启动推荐 — 纯热度 + 流派多样性
    /// </summary>
    public static List<DiscoveryTrack> ColdStart(List<Track> candidates, int limit)
    {
        return SampleWithDiversity(candidates, limit, preferredGenres: null);
    }

    /// <summary>
    /// 个性化探索推荐 — 70% 热门 + 20% 偏好流派 + 10% 随机新风格
    /// </summary>
    /// <remarks>
    /// 70/20/10 分配的设计理由：
    ///   - 70% 热门：确保推荐质量不因"过度探索"而大幅下降
    ///   - 20% 偏好：在用户舒适区内扩展（已知喜欢的流派但没听过的曲目）
    ///   - 10% 随机：打破推荐茧房，引入用户从未接触的风格（可能发现新喜好）
    ///
    /// 此比例可通过调整常量轻松定制（如改为 60/30/10 更偏探索，80/15/5 更偏保守）。
    /// </remarks>
    public static List<DiscoveryTrack> Explore(
        List<Track> candidates, int limit, HashSet<string> preferredGenres)
    {
        var hotCount = (int)(limit * 0.70);
        var genreCount = (int)(limit * 0.20);
        var randomCount = limit - hotCount - genreCount;

        var result = new List<DiscoveryTrack>();
        var usedTrackIds = new HashSet<Guid>();

        // 70%：热门曲目（按流行度降序）
        foreach (var track in candidates.OrderByDescending(t => t.Popularity))
        {
            if (result.Count >= hotCount) break;
            if (!usedTrackIds.Add(track.Id)) continue;
            result.Add(new DiscoveryTrack(track, "热门推荐"));
        }

        // 20%：偏好流派采样
        var remaining = candidates.Where(t => !usedTrackIds.Contains(t.Id)).ToList();
        var genrePicks = SampleWithDiversity(remaining, genreCount, preferredGenres);
        foreach (var pick in genrePicks)
        {
            if (!usedTrackIds.Add(pick.Track.Id)) continue;
            result.Add(pick);
        }

        // 10%：完全随机新风格
        var randomPicks = candidates
            .Where(t => !usedTrackIds.Contains(t.Id))
            .OrderBy(_ => Random.Shared.Next())
            .Take(randomCount)
            .Select(t => new DiscoveryTrack(t, "探索新风格 · 发现未知音乐"));
        result.AddRange(randomPicks);

        return result.Take(limit).ToList();
    }
}

/// <summary>
/// 探索曲目内部记录 — Track + 探索原因
/// </summary>
public record DiscoveryTrack(Track Track, string DiscoveryReason);
