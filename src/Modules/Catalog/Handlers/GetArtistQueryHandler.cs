using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Entities;
using MusicRec.Catalog.Queries;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Catalog.Handlers;

public class GetArtistQueryHandler : IRequestHandler<GetArtistQuery, ArtistDto>
{
    private readonly MusicRecDbContext _db;

    public GetArtistQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<ArtistDto> Handle(GetArtistQuery request, CancellationToken ct)
    {
        var artist = await _db.Set<Artist>()
            .FirstOrDefaultAsync(a => a.Id == request.ArtistId, ct);

        if (artist is null)
            throw new NotFoundException("艺术家不存在");

        return artist.Adapt<ArtistDto>();
    }
}
