using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.AudioFeatures.DTOs;
using MusicRec.AudioFeatures.Entities;
using MusicRec.AudioFeatures.Queries;
using MusicRec.Infrastructure;
using MusicRec.Spotify;

namespace MusicRec.AudioFeatures.Handlers;

/// <summary>
/// 查询音频特征：Cache-Aside 模式 — 先查本地 DB，未命中则调 Spotify API 并落库
/// </summary>
/// <remarks>
/// 使用 Mapster 映射 AudioFeaturesObject → TrackAudioFeature（配置于 AudioFeatureMapping），
/// 避免逐字段赋值。
/// </remarks>
public class GetAudioFeaturesQueryHandler : IRequestHandler<GetAudioFeaturesQuery, AudioFeaturesDto?>
{
    private readonly MusicRecDbContext _db;
    private readonly ISpotifyClient _spotify;

    public GetAudioFeaturesQueryHandler(MusicRecDbContext db, ISpotifyClient spotify)
    {
        _db = db;
        _spotify = spotify;
    }

    public async Task<AudioFeaturesDto?> Handle(GetAudioFeaturesQuery request, CancellationToken ct)
    {
        var local = await _db.Set<TrackAudioFeature>()
            .FirstOrDefaultAsync(af => af.SpotifyTrackId == request.SpotifyTrackId, ct);

        if (local is not null)
            return local.Adapt<AudioFeaturesDto>();

        // 本地未命中，从 Spotify 获取
        var af = await _spotify.GetAudioFeaturesAsync(request.SpotifyTrackId, ct);
        if (af is null) return null; // Spotify 尚未分析该曲目

        var entity = af.Adapt<TrackAudioFeature>(); // Mapster 映射，消除 13 行手动赋值
        _db.Set<TrackAudioFeature>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.Adapt<AudioFeaturesDto>();
    }
}
