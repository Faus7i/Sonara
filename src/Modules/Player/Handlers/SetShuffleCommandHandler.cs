using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class SetShuffleCommandHandler : IRequestHandler<SetShuffleCommand>
{
    private readonly ISpotifyClient _spotify;

    public SetShuffleCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(SetShuffleCommand request, CancellationToken ct)
    {
        await _spotify.SetShuffleAsync(request.State, request.DeviceId, ct);
    }
}
