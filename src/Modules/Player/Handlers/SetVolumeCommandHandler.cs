using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class SetVolumeCommandHandler : IRequestHandler<SetVolumeCommand>
{
    private readonly ISpotifyClient _spotify;

    public SetVolumeCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(SetVolumeCommand request, CancellationToken ct)
    {
        await _spotify.SetVolumeAsync(request.VolumePercent, request.DeviceId, ct);
    }
}
