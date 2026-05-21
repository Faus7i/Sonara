using Mapster;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Entities;

namespace MusicRec.Catalog;

/// <summary>
/// Catalog 模块的 Mapster 映射配置
/// </summary>
/// <remarks>
/// Track.TrackArtists → TrackDto.Artists 需要显式配置，
/// 因为 Mapster 无法自动推断 TrackArtist.Artist → ArtistBriefDto 的嵌套路径。
/// 映射时要求 TrackArtists 导航属性已被 Include/ThenInclude 加载。
///
/// TrackAudioFeature → AudioFeaturesBriefDto 无需自定义映射：
/// 源和目标属性名完全一致（10 个音频特征字段），Mapster 按名称自动匹配。
/// TrackAudioFeature 多余字段（Liveness、TimeSignature、DurationMs）自动忽略。
/// </remarks>
public static class CatalogMapping
{
    public static void Configure()
    {
        TypeAdapterConfig<Track, TrackDto>.NewConfig()
            .Map(dest => dest.Artists,
                src => src.TrackArtists != null
                    ? src.TrackArtists.Select(ta => ta.Artist.Adapt<ArtistBriefDto>()).ToList()
                    : new List<ArtistBriefDto>());

        // TrackAudioFeature 与 AudioFeaturesBriefDto 属性名一一对应，默认映射即可
        TypeAdapterConfig<TrackAudioFeature, AudioFeaturesBriefDto>.NewConfig();
    }
}
