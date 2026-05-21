using MediatR;
using MusicRec.Player.Commands;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

public class TransferPlaybackCommandHandler : IRequestHandler<TransferPlaybackCommand>
{
    private readonly ISpotifyClient _spotify;

    public TransferPlaybackCommandHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task Handle(TransferPlaybackCommand request, CancellationToken ct)
    {
        await _spotify.TransferPlaybackAsync(request.DeviceId, request.Play, ct);
    }
}
