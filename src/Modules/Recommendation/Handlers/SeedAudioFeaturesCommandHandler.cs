using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Catalog.Entities;
using MusicRec.Infrastructure;
using MusicRec.Recommendation.Commands;

namespace MusicRec.Recommendation.Handlers;

/// <summary>
/// 种子数据生成器 — 为缺少音频特征的曲目基于流派模板生成模拟 TrackAudioFeature
/// </summary>
/// <remarks>
/// 使用流派→音频特征映射表 + 确定性随机（以 Track.Id 为种子），
/// 确保同一曲目每次生成的特征值一致。
/// 只处理 TrackAudioFeatures 表中尚不存在的曲目（幂等）。
/// 每 500 条批量插入以控制 SQL 命令大小。
///
/// 此 Handler 是 Spotify Audio Features API 封锁期间的临时方案。
/// API 恢复后只需改用真实数据覆盖即可，推荐算法层无需任何修改。
/// </remarks>
public class SeedAudioFeaturesCommandHandler : IRequestHandler<SeedAudioFeaturesCommand, SeedAudioFeaturesResult>
{
    private readonly MusicRecDbContext _db;
    private readonly ILogger<SeedAudioFeaturesCommandHandler> _logger;

    public SeedAudioFeaturesCommandHandler(MusicRecDbContext db, ILogger<SeedAudioFeaturesCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SeedAudioFeaturesResult> Handle(SeedAudioFeaturesCommand request, CancellationToken ct)
    {
        // 阶段 1：查找所有缺少音频特征的曲目
        var existingSpotifyIds = await _db.Set<TrackAudioFeature>()
            .Select(af => af.SpotifyTrackId)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existingSpotifyIds);

        var unmatchedTracks = await _db.Set<Track>()
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .Include(t => t.TrackGenres).ThenInclude(tg => tg.Genre)
            .Where(t => !existingSet.Contains(t.SpotifyTrackId))
            .ToListAsync(ct);

        if (unmatchedTracks.Count == 0)
        {
            _logger.LogInformation("种子数据生成：所有曲目已拥有音频特征，无需处理");
            return new SeedAudioFeaturesResult(0, 0);
        }

        _logger.LogInformation("种子数据生成：发现 {Count} 首曲目缺少音频特征，开始生成模拟数据",
            unmatchedTracks.Count);

        // 阶段 2：为每首曲目生成模拟音频特征
        var features = new List<TrackAudioFeature>();
        foreach (var track in unmatchedTracks)
        {
            var genreNames = ExtractGenreNames(track);
            var template = SelectTemplate(genreNames);
            var rng = new Random(track.Id.GetHashCode()); // 确定性种子

            var af = new TrackAudioFeature
            {
                Id = Guid.NewGuid(),
                SpotifyTrackId = track.SpotifyTrackId,
                Energy = RandomInRange(template.Energy, rng),
                Danceability = RandomInRange(template.Danceability, rng),
                Valence = RandomInRange(template.Valence, rng),
                Tempo = RandomInRange(template.Tempo, rng),
                Acousticness = RandomInRange(template.Acousticness, rng),
                Instrumentalness = RandomInRange(template.Instrumentalness, rng),
                Speechiness = RandomInRange(template.Speechiness, rng),
                Liveness = RandomInRange(template.Liveness, rng),
                Key = rng.Next(0, 12),
                Mode = rng.Next(0, 2),
                Loudness = -20f + (float)rng.NextDouble() * 15f, // -20 到 -5 dB
                TimeSignature = rng.NextDouble() switch { < 0.1 => 3, < 0.9 => 4, _ => 6 },
                DurationMs = track.DurationMs
            };

            features.Add(af);
        }

        // 阶段 3：批量插入（每 500 条一批，避免 EF ChangeTracker 内存膨胀）
        const int batchSize = 500;
        var created = 0;
        for (var i = 0; i < features.Count; i += batchSize)
        {
            var batch = features.Skip(i).Take(batchSize).ToList();
            _db.Set<TrackAudioFeature>().AddRange(batch);
            await _db.SaveChangesAsync(ct);
            created += batch.Count;
            _logger.LogDebug("种子数据：已插入 {Created}/{Total} 条音频特征", created, features.Count);
        }

        _logger.LogInformation("种子数据生成完成：处理 {TrackCount} 首曲目，创建 {FeatureCount} 条音频特征",
            unmatchedTracks.Count, created);

        return new SeedAudioFeaturesResult(unmatchedTracks.Count, created);
    }

    /// <summary>
    /// 从曲目的艺术家流派中提取流派名称列表
    /// </summary>
    private static List<string> ExtractGenreNames(Track track)
    {
        var genres = new List<string>();

        // 方式 1：从 Track.Genres 多对多表获取
        if (track.TrackGenres is { Count: > 0 })
            genres.AddRange(track.TrackGenres.Select(tg => tg.Genre!.Name));

        // 方式 2：从艺术家的 Genres 逗号分隔字段获取
        if (track.TrackArtists is { Count: > 0 })
        {
            foreach (var ta in track.TrackArtists)
            {
                if (!string.IsNullOrEmpty(ta.Artist?.Genres))
                    genres.AddRange(ta.Artist.Genres.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim()));
            }
        }

        return genres.Distinct().ToList();
    }

    /// <summary>
    /// 根据流派名称列表匹配模板，优先匹配第一个识别的流派
    /// </summary>
    private static GenreAudioTemplate SelectTemplate(List<string> genreNames)
    {
        foreach (var name in genreNames)
        {
            var key = name.ToLowerInvariant();
            if (GenreTemplates.TryGetValue(key, out var template))
                return template;

            // 部分匹配：如 "pop rock" 匹配 "rock"
            foreach (var (templateKey, t) in GenreTemplates)
            {
                if (key.Contains(templateKey) || templateKey.Contains(key))
                    return t;
            }
        }

        return DefaultTemplate;
    }

    /// <summary>
    /// 在 [min, max] 范围内生成随机值
    /// </summary>
    /// <remarks>
    /// 在流派模板范围中点 ± 半宽范围内均匀采样，然后用 Math.Clamp 确保不超出 [0, 1]。
    /// 使用确定性 Random（以 Track.Id 为种子），保证同一曲目每次运行生成的特征值完全相同。
    /// 这确保了：
    ///   1. 测试可重现性 — 同一输入始终得到相同的推荐结果
    ///   2. 幂等性 — 重复调用 seed 端点不会创建重复特征（已有特征直接跳过）
    /// </remarks>
    private static float RandomInRange((float Min, float Max) range, Random rng)
    {
        var mid = (range.Min + range.Max) / 2f;
        var halfWidth = (range.Max - range.Min) / 2f;
        var value = (float)(mid + (rng.NextDouble() * 2 - 1) * halfWidth);
        return Math.Clamp(value, 0f, 1f);
    }

    // ─── 流派→音频特征模板 ──────────────────────────

    private record GenreAudioTemplate(
        (float Min, float Max) Energy,
        (float Min, float Max) Danceability,
        (float Min, float Max) Valence,
        (float Min, float Max) Tempo,
        (float Min, float Max) Acousticness,
        (float Min, float Max) Instrumentalness,
        (float Min, float Max) Speechiness,
        (float Min, float Max) Liveness
    );

    private static readonly GenreAudioTemplate DefaultTemplate = new(
        Energy: (0.5f, 0.7f), Danceability: (0.5f, 0.7f), Valence: (0.4f, 0.6f),
        Tempo: (100f, 120f), Acousticness: (0.2f, 0.4f),
        Instrumentalness: (0.0f, 0.2f), Speechiness: (0.0f, 0.2f), Liveness: (0.1f, 0.3f)
    );

    // 20+ 流派模板，基于音乐学常识与 Spotify 统计分布
    private static readonly Dictionary<string, GenreAudioTemplate> GenreTemplates = new()
    {
        ["electronic"] = new(Energy: (0.7f, 0.9f), Danceability: (0.7f, 0.9f), Valence: (0.3f, 0.7f),
            Tempo: (120f, 140f), Acousticness: (0.0f, 0.2f),
            Instrumentalness: (0.2f, 0.6f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.3f)),
        ["edm"] = new(Energy: (0.8f, 1.0f), Danceability: (0.6f, 0.8f), Valence: (0.3f, 0.6f),
            Tempo: (125f, 150f), Acousticness: (0.0f, 0.2f),
            Instrumentalness: (0.2f, 0.5f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.3f)),
        ["house"] = new(Energy: (0.6f, 0.8f), Danceability: (0.7f, 0.9f), Valence: (0.4f, 0.7f),
            Tempo: (118f, 128f), Acousticness: (0.0f, 0.1f),
            Instrumentalness: (0.2f, 0.5f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.2f)),
        ["techno"] = new(Energy: (0.7f, 0.9f), Danceability: (0.6f, 0.8f), Valence: (0.2f, 0.4f),
            Tempo: (125f, 140f), Acousticness: (0.0f, 0.1f),
            Instrumentalness: (0.5f, 0.9f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.2f)),
        ["dance"] = new(Energy: (0.7f, 0.9f), Danceability: (0.8f, 1.0f), Valence: (0.5f, 0.8f),
            Tempo: (120f, 135f), Acousticness: (0.0f, 0.1f),
            Instrumentalness: (0.1f, 0.3f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.2f)),
        ["pop"] = new(Energy: (0.6f, 0.8f), Danceability: (0.6f, 0.8f), Valence: (0.5f, 0.8f),
            Tempo: (100f, 130f), Acousticness: (0.1f, 0.3f),
            Instrumentalness: (0.0f, 0.1f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.2f)),
        ["rock"] = new(Energy: (0.7f, 0.9f), Danceability: (0.4f, 0.6f), Valence: (0.4f, 0.7f),
            Tempo: (100f, 140f), Acousticness: (0.1f, 0.3f),
            Instrumentalness: (0.1f, 0.3f), Speechiness: (0.0f, 0.1f), Liveness: (0.2f, 0.4f)),
        ["metal"] = new(Energy: (0.8f, 1.0f), Danceability: (0.3f, 0.5f), Valence: (0.2f, 0.5f),
            Tempo: (100f, 180f), Acousticness: (0.0f, 0.1f),
            Instrumentalness: (0.1f, 0.3f), Speechiness: (0.0f, 0.1f), Liveness: (0.2f, 0.4f)),
        ["hip hop"] = new(Energy: (0.6f, 0.8f), Danceability: (0.7f, 0.9f), Valence: (0.3f, 0.6f),
            Tempo: (80f, 120f), Acousticness: (0.0f, 0.2f),
            Instrumentalness: (0.0f, 0.1f), Speechiness: (0.2f, 0.4f), Liveness: (0.1f, 0.3f)),
        ["rap"] = new(Energy: (0.5f, 0.7f), Danceability: (0.7f, 0.9f), Valence: (0.3f, 0.5f),
            Tempo: (80f, 115f), Acousticness: (0.0f, 0.1f),
            Instrumentalness: (0.0f, 0.1f), Speechiness: (0.3f, 0.5f), Liveness: (0.1f, 0.2f)),
        ["r&b"] = new(Energy: (0.4f, 0.6f), Danceability: (0.5f, 0.7f), Valence: (0.5f, 0.7f),
            Tempo: (60f, 100f), Acousticness: (0.2f, 0.4f),
            Instrumentalness: (0.0f, 0.2f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.2f)),
        ["jazz"] = new(Energy: (0.3f, 0.6f), Danceability: (0.3f, 0.6f), Valence: (0.4f, 0.7f),
            Tempo: (60f, 120f), Acousticness: (0.3f, 0.7f),
            Instrumentalness: (0.3f, 0.8f), Speechiness: (0.0f, 0.1f), Liveness: (0.2f, 0.4f)),
        ["classical"] = new(Energy: (0.1f, 0.3f), Danceability: (0.1f, 0.3f), Valence: (0.3f, 0.7f),
            Tempo: (60f, 90f), Acousticness: (0.6f, 0.9f),
            Instrumentalness: (0.7f, 0.9f), Speechiness: (0.0f, 0.0f), Liveness: (0.1f, 0.2f)),
        ["folk"] = new(Energy: (0.3f, 0.5f), Danceability: (0.3f, 0.5f), Valence: (0.4f, 0.7f),
            Tempo: (80f, 120f), Acousticness: (0.5f, 0.8f),
            Instrumentalness: (0.2f, 0.5f), Speechiness: (0.0f, 0.1f), Liveness: (0.2f, 0.4f)),
        ["country"] = new(Energy: (0.4f, 0.6f), Danceability: (0.4f, 0.6f), Valence: (0.5f, 0.7f),
            Tempo: (100f, 130f), Acousticness: (0.3f, 0.6f),
            Instrumentalness: (0.0f, 0.2f), Speechiness: (0.0f, 0.1f), Liveness: (0.2f, 0.3f)),
        ["indie"] = new(Energy: (0.4f, 0.7f), Danceability: (0.4f, 0.6f), Valence: (0.4f, 0.6f),
            Tempo: (100f, 140f), Acousticness: (0.2f, 0.5f),
            Instrumentalness: (0.1f, 0.4f), Speechiness: (0.0f, 0.1f), Liveness: (0.2f, 0.3f)),
        ["ambient"] = new(Energy: (0.1f, 0.3f), Danceability: (0.1f, 0.2f), Valence: (0.2f, 0.5f),
            Tempo: (60f, 100f), Acousticness: (0.4f, 0.8f),
            Instrumentalness: (0.5f, 0.9f), Speechiness: (0.0f, 0.0f), Liveness: (0.1f, 0.2f)),
        ["blues"] = new(Energy: (0.3f, 0.5f), Danceability: (0.3f, 0.5f), Valence: (0.3f, 0.6f),
            Tempo: (60f, 100f), Acousticness: (0.4f, 0.7f),
            Instrumentalness: (0.2f, 0.5f), Speechiness: (0.0f, 0.1f), Liveness: (0.2f, 0.4f)),
        ["punk"] = new(Energy: (0.7f, 0.9f), Danceability: (0.3f, 0.5f), Valence: (0.3f, 0.6f),
            Tempo: (160f, 220f), Acousticness: (0.0f, 0.2f),
            Instrumentalness: (0.0f, 0.2f), Speechiness: (0.0f, 0.1f), Liveness: (0.3f, 0.5f)),
        ["reggae"] = new(Energy: (0.4f, 0.6f), Danceability: (0.5f, 0.7f), Valence: (0.5f, 0.8f),
            Tempo: (80f, 100f), Acousticness: (0.2f, 0.4f),
            Instrumentalness: (0.1f, 0.3f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.3f)),
        ["funk"] = new(Energy: (0.5f, 0.7f), Danceability: (0.7f, 0.9f), Valence: (0.6f, 0.8f),
            Tempo: (100f, 120f), Acousticness: (0.1f, 0.3f),
            Instrumentalness: (0.0f, 0.2f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.3f)),
        ["soul"] = new(Energy: (0.4f, 0.6f), Danceability: (0.4f, 0.6f), Valence: (0.5f, 0.8f),
            Tempo: (80f, 110f), Acousticness: (0.2f, 0.5f),
            Instrumentalness: (0.0f, 0.2f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.2f)),
        ["latin"] = new(Energy: (0.6f, 0.8f), Danceability: (0.6f, 0.8f), Valence: (0.5f, 0.8f),
            Tempo: (100f, 130f), Acousticness: (0.1f, 0.3f),
            Instrumentalness: (0.0f, 0.1f), Speechiness: (0.0f, 0.1f), Liveness: (0.1f, 0.3f)),
    };
}
