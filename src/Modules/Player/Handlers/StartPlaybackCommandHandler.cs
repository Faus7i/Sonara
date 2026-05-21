using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class StartPlaybackCommandHandler : IRequestHandler<StartPlaybackCommand>
{
    private readonly ISpotifyClient _spotify;

    public StartPlaybackCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(StartPlaybackCommand request, CancellationToken ct)
    {
        await _spotify.StartPlaybackAsync(
            request.DeviceId, request.Uris, request.ContextUri, request.PositionMs, ct);
    }
}
