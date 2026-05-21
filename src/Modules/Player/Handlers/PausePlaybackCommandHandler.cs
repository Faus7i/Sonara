using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class PausePlaybackCommandHandler : IRequestHandler<PausePlaybackCommand>
{
    private readonly ISpotifyClient _spotify;

    public PausePlaybackCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(PausePlaybackCommand request, CancellationToken ct)
    {
        await _spotify.PausePlaybackAsync(request.DeviceId, ct);
    }
}
