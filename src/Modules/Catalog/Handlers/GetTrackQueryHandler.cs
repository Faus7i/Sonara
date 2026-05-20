using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Entities;
using MusicRec.Catalog.Queries;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Catalog.Handlers;

public class GetTrackQueryHandler : IRequestHandler<GetTrackQuery, TrackDto>
{
    private readonly MusicRecDbContext _db;

    public GetTrackQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<TrackDto> Handle(GetTrackQuery request, CancellationToken ct)
    {
        var track = await _db.Set<Track>()
            .Include(t => t.Album)
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .FirstOrDefaultAsync(t => t.Id == request.TrackId, ct);

        if (track is null)
            throw new NotFoundException("曲目不存在");

        return track.Adapt<TrackDto>();
    }
}
