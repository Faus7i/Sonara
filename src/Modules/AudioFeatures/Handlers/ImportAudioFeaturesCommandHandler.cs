using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.AudioFeatures.Commands;
using MusicRec.AudioFeatures.DTOs;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Infrastructure;
using MusicRec.Spotify;
using MusicRec.Spotify.Models;

namespace MusicRec.AudioFeatures.Handlers;

/// <summary>
/// 批量导入音频特征：从 Spotify 获取后写入本地 DB，已存在的跳过
/// </summary>
/// <remarks>
/// Spotify Audio Features API 每批次限制 100 个 ID，超出需分批处理。
/// 使用 Mapster 映射 AudioFeaturesObject → TrackAudioFeature（注册于 AudioFeatureMapping），
/// 避免逐字段手动赋值 13 个属性。
/// </remarks>
public class ImportAudioFeaturesCommandHandler : IRequestHandler<ImportAudioFeaturesCommand, IReadOnlyList<AudioFeaturesDto>>
{
    private const int SpotifyBatchLimit = 100; // Spotify 批量音频特征 API 单次上限

    private readonly MusicRecDbContext _db;
    private readonly ISpotifyClient _spotify;

    public ImportAudioFeaturesCommandHandler(MusicRecDbContext db, ISpotifyClient spotify)
    {
        _db = db;
        _spotify = spotify;
    }

    public async Task<IReadOnlyList<AudioFeaturesDto>> Handle(ImportAudioFeaturesCommand request, CancellationToken ct)
    {
        // 过滤掉本地已存在的曲目，避免重复 API 调用
        var existingIds = await _db.Set<TrackAudioFeature>()
            .Where(af => request.SpotifyTrackIds.Contains(af.SpotifyTrackId))
            .Select(af => af.SpotifyTrackId)
            .ToListAsync(ct);

        var missingIds = request.SpotifyTrackIds.Except(existingIds).ToList();
        if (missingIds.Count == 0)
        {
            var existing = await _db.Set<TrackAudioFeature>()
                .Where(af => request.SpotifyTrackIds.Contains(af.SpotifyTrackId))
                .ToListAsync(ct);
            return existing.Adapt<IReadOnlyList<AudioFeaturesDto>>();
        }

        // 分批获取 Spotify 音频特征，每批最多 100 个
        var entities = new List<TrackAudioFeature>();
        for (var i = 0; i < missingIds.Count; i += SpotifyBatchLimit)
        {
            var batch = missingIds.Skip(i).Take(SpotifyBatchLimit).ToList();
            var features = await _spotify.GetAudioFeaturesBatchAsync(batch, ct);

            foreach (var af in features)
            {
                var entity = af.Adapt<TrackAudioFeature>(); // Mapster 映射，消除 13 行手动赋值
                _db.Set<TrackAudioFeature>().Add(entity);
                entities.Add(entity);
            }
        }

        // 保存后映射 DTO，确保 Id 为数据库最终值
        await _db.SaveChangesAsync(ct);
        return entities.Adapt<IReadOnlyList<AudioFeaturesDto>>();
    }
}
