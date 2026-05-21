using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Entities;
using MusicRec.Catalog.Queries;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Catalog.Handlers;

public class GetAlbumQueryHandler : IRequestHandler<GetAlbumQuery, AlbumDto>
{
    private readonly MusicRecDbContext _db;

    public GetAlbumQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<AlbumDto> Handle(GetAlbumQuery request, CancellationToken ct)
    {
        // 单次查询加载 Album + Tracks + TrackArtists + Artists，避免 N+1 和二次往返
        var album = await _db.Set<Album>()
            .Include(a => a.Tracks)
                .ThenInclude(t => t.TrackArtists)
                    .ThenInclude(ta => ta.Artist)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AlbumId, ct);

        if (album is null)
            throw new NotFoundException("专辑不存在");

        // 通过 Track→TrackArtist→Artist 链提取去重艺术家列表
        var artists = album.Tracks
            .SelectMany(t => t.TrackArtists)
            .Select(ta => ta.Artist)
            .Distinct()
            .ToList();

        var tracksDto = album.Tracks.Select(t => t.Adapt<TrackBriefDto>()).ToList();
        var artistsDto = artists.Select(a => a.Adapt<ArtistBriefDto>()).ToList();

        // 直接构造 DTO，避免 Adapt + with 的两次对象分配
        return new AlbumDto(
            album.Id,
            album.SpotifyAlbumId,
            album.Name,
            album.ReleaseDate,
            album.CoverImageUrl,
            album.AlbumType,
            album.TotalTracks,
            artistsDto,
            tracksDto
        );
    }
}
