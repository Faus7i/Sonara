using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class SeekToPositionCommandHandler : IRequestHandler<SeekToPositionCommand>
{
    private readonly ISpotifyClient _spotify;

    public SeekToPositionCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(SeekToPositionCommand request, CancellationToken ct)
    {
        await _spotify.SeekToPositionAsync(request.PositionMs, request.DeviceId, ct);
    }
}
