using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class SkipToNextCommandHandler : IRequestHandler<SkipToNextCommand>
{
    private readonly ISpotifyClient _spotify;

    public SkipToNextCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(SkipToNextCommand request, CancellationToken ct)
    {
        await _spotify.SkipToNextAsync(request.DeviceId, ct);
    }
}
