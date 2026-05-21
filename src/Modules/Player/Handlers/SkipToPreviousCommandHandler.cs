using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class SkipToPreviousCommandHandler : IRequestHandler<SkipToPreviousCommand>
{
    private readonly ISpotifyClient _spotify;

    public SkipToPreviousCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(SkipToPreviousCommand request, CancellationToken ct)
    {
        await _spotify.SkipToPreviousAsync(request.DeviceId, ct);
    }
}
