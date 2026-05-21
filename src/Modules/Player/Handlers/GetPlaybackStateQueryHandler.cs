using MediatR;
using MusicRec.Player.DTOs;
using MusicRec.Player.Queries;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

/// <summary>
/// 获取播放状态处理器 — 无活跃播放时返回 null
/// </summary>
public class GetPlaybackStateQueryHandler : IRequestHandler<GetPlaybackStateQuery, PlaybackStateDto?>
{
    private readonly ISpotifyClient _spotify;

    public GetPlaybackStateQueryHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task<PlaybackStateDto?> Handle(GetPlaybackStateQuery request, CancellationToken ct)
    {
        var state = await _spotify.GetPlaybackStateAsync(ct);
        if (state is null)
            return null; // 无活跃播放会话

        return new PlaybackStateDto(
            IsPlaying: state.IsPlaying,
            ProgressMs: state.ProgressMs,
            RepeatState: state.RepeatState,
            ShuffleState: state.ShuffleState,
            ContextType: state.Context?.Type,
            ContextUri: state.Context?.Uri,
            CurrentTrack: MapTrack(state.Item),
            Device: state.Device is not null
                ? new DeviceDto(state.Device.Id, state.Device.Name, state.Device.Type,
                    state.Device.IsActive, state.Device.VolumePercent)
                : null
        );
    }

    /// <summary>将 Spotify TrackObject 映射为前端播放器需要的曲目摘要</summary>
    private static TrackBriefDto? MapTrack(Spotify.Models.TrackObject? item)
    {
        if (item is null) return null;
        return new TrackBriefDto(
            SpotifyId: item.Id,
            Name: item.Name,
            ArtistsSummary: item.Artists?.Count > 0
                ? string.Join(", ", item.Artists.Select(a => a.Name))
                : "未知艺术家",
            AlbumName: item.Album?.Name,
            CoverImageUrl: item.Album?.Images?.FirstOrDefault()?.Url,
            DurationMs: item.DurationMs
        );
    }
}
