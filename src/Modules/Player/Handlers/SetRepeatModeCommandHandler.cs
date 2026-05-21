using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class SetRepeatModeCommandHandler : IRequestHandler<SetRepeatModeCommand>
{
    private readonly ISpotifyClient _spotify;

    public SetRepeatModeCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(SetRepeatModeCommand request, CancellationToken ct)
    {
        await _spotify.SetRepeatModeAsync(request.State, request.DeviceId, ct);
    }
}
