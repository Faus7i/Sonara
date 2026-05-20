namespace MusicRec.AudioFeatures.DTOs;

/// <summary>
/// 音频特征 DTO — 包含 10 个音频特征维度
/// </summary>
public record AudioFeaturesDto(
    Guid Id,
    string SpotifyTrackId,
    float Acousticness,
    float Danceability,
    float Energy,
    float Instrumentalness,
    int Key,
    float Liveness,
    float Loudness,
    int Mode,
    float Speechiness,
    float Tempo,
    int TimeSignature,
    float Valence,
    int DurationMs
);
