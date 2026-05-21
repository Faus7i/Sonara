using Mapster;
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
    }
}
