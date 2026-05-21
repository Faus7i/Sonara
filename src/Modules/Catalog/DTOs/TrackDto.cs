namespace MusicRec.Catalog.DTOs;

/// <summary>
/// 曲目详情 DTO — 含艺术家、专辑及音频特征（可选）
/// </summary>
/// <remarks>
/// AudioFeatures 为可选参数，默认 null。
/// 仅在详情查询（GetTrackQueryHandler）中填充，导入等其他场景保持 null。
/// 使用 Mapster Adapt 时，源 Track 实体无此字段，自动取默认值 null。
/// </remarks>
public record TrackDto(
    Guid Id,
    string SpotifyTrackId,
    string Name,
    int DurationMs,
    int Popularity,
    string? ReleaseDate,
    string? CoverImageUrl,
    AlbumBriefDto Album,
    IReadOnlyList<ArtistBriefDto> Artists,
    AudioFeaturesBriefDto? AudioFeatures = null
);

/// <summary>
/// 音频特征摘要 DTO — 用于曲目详情页展示
/// </summary>
public record AudioFeaturesBriefDto(
    float Acousticness,
    float Danceability,
    float Energy,
    float Instrumentalness,
    int Key,
    float Loudness,
    int Mode,
    float Speechiness,
    float Tempo,
    float Valence
);

public record ArtistBriefDto(
    Guid Id,
    string SpotifyArtistId,
    string Name,
    string? Genres,
    string? ImageUrl
);

public record AlbumBriefDto(
    Guid Id,
    string SpotifyAlbumId,
    string Name,
    string? ReleaseDate,
    string? CoverImageUrl,
    string? AlbumType
);
