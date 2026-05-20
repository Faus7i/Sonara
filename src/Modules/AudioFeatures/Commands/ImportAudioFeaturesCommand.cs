using MediatR;
using MusicRec.AudioFeatures.DTOs;

namespace MusicRec.AudioFeatures.Commands;

/// <summary>
/// 批量导入 Spotify 音频特征 — 支持一次导入最多 100 个曲目
/// </summary>
public record ImportAudioFeaturesCommand(IReadOnlyList<string> SpotifyTrackIds) : IRequest<IReadOnlyList<AudioFeaturesDto>>;
