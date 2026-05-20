using Mapster;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Spotify.Models;

namespace MusicRec.AudioFeatures;

/// <summary>
/// AudioFeatures 相关的 Mapster 映射配置
/// </summary>
/// <remarks>
/// 避免在 Handler 中逐字段手动赋值 13 个音频特征属性。
/// AudioFeaturesObject.Id（Spotify Track ID）映射到 TrackAudioFeature.SpotifyTrackId，
/// 其余同名属性由 Mapster 自动映射。
/// </remarks>
public static class AudioFeatureMapping
{
    public static void Configure()
    {
        TypeAdapterConfig<AudioFeaturesObject, TrackAudioFeature>.NewConfig()
            .Map(dest => dest.SpotifyTrackId, src => src.Id)
            .Ignore(dest => dest.Id); // Id 由 SaveChangesAsync 自动分配
    }
}
