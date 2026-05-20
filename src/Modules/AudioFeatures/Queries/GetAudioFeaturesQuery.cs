using MediatR;
using MusicRec.AudioFeatures.DTOs;

namespace MusicRec.AudioFeatures.Queries;

/// <summary>
/// 查询曲目音频特征（先查本地，未命中则从 Spotify 获取）
/// </summary>
public record GetAudioFeaturesQuery(string SpotifyTrackId) : IRequest<AudioFeaturesDto?>;
