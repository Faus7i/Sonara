using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.Commands;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;
using MusicRec.Spotify;
using MusicRec.Spotify.Models;

namespace MusicRec.Catalog.Handlers;

/// <summary>
/// 从 Spotify 导入曲目：先查本地 DB，未命中则调 Spotify API 获取并落库
/// </summary>
/// <remarks>
/// 导入策略：Cache-Aside 模式 — 先查本地 DB，命中直接返回；
/// 未命中则调用 Spotify API 获取完整数据（含专辑/艺术家），写入本地后返回。
/// 所有关联数据（Track/Album/Artist/TrackArtist）在单次 SaveChangesAsync 中完成，保证事务性。
/// </remarks>
public class ImportTrackCommandHandler : IRequestHandler<ImportTrackCommand, TrackDto>
{
    private readonly MusicRecDbContext _db;
    private readonly ISpotifyClient _spotify;

    public ImportTrackCommandHandler(MusicRecDbContext db, ISpotifyClient spotify)
    {
        _db = db;
        _spotify = spotify;
    }

    public async Task<TrackDto> Handle(ImportTrackCommand request, CancellationToken ct)
    {
        // Cache-Aside：先查本地，降低 Spotify API 调用量和响应延迟
        var existing = await _db.Set<Track>()
            .Include(t => t.Album)
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .FirstOrDefaultAsync(t => t.SpotifyTrackId == request.SpotifyTrackId, ct);

        if (existing is not null)
            return existing.Adapt<TrackDto>();

        var trackObj = await _spotify.GetTrackAsync(request.SpotifyTrackId, ct);

        // 导入或复用专辑
        var album = await GetOrCreateAlbumAsync(trackObj.Album, ct);

        // 导入或复用艺术家
        var artists = await GetOrCreateArtistsAsync(trackObj.Artists, ct);

        // 提前分配 Id，使 TrackArtist 能在同一次 SaveChanges 中关联
        var track = new Track
        {
            Id = Guid.NewGuid(),
            SpotifyTrackId = trackObj.Id,
            Name = trackObj.Name,
            AlbumId = album.Id,
            DurationMs = trackObj.DurationMs,
            Popularity = trackObj.Popularity,
            ReleaseDate = trackObj.Album.ReleaseDate,
            CoverImageUrl = trackObj.Album.Images.FirstOrDefault()?.Url
        };
        _db.Set<Track>().Add(track);

        foreach (var artist in artists)
            _db.Set<TrackArtist>().Add(new TrackArtist { TrackId = track.Id, ArtistId = artist.Id });

        // 单次保存所有变更（Track + TrackArtists），一个数据库事务
        await _db.SaveChangesAsync(ct);

        // 填充导航属性用于 Mapster 映射
        track.Album = album;
        track.TrackArtists = artists.Select(a => new TrackArtist { TrackId = track.Id, ArtistId = a.Id, Artist = a }).ToList();
        return track.Adapt<TrackDto>();
    }

    private async Task<Album> GetOrCreateAlbumAsync(SimplifiedAlbumObject albumObj, CancellationToken ct)
    {
        var album = await _db.Set<Album>()
            .FirstOrDefaultAsync(a => a.SpotifyAlbumId == albumObj.Id, ct);
        if (album is not null) return album;

        album = new Album
        {
            Id = Guid.NewGuid(),
            SpotifyAlbumId = albumObj.Id,
            Name = albumObj.Name,
            ReleaseDate = albumObj.ReleaseDate,
            CoverImageUrl = albumObj.Images.FirstOrDefault()?.Url,
            AlbumType = albumObj.AlbumType,
            TotalTracks = albumObj.TotalTracks
        };
        _db.Set<Album>().Add(album);
        return album;
    }

    private async Task<List<Artist>> GetOrCreateArtistsAsync(
        List<Spotify.Models.SimplifiedArtistObject> artistObjs, CancellationToken ct)
    {
        var artists = new List<Artist>();
        foreach (var obj in artistObjs)
        {
            var artist = await _db.Set<Artist>()
                .FirstOrDefaultAsync(a => a.SpotifyArtistId == obj.Id, ct);
            if (artist is null)
            {
                artist = new Artist
                {
                    Id = Guid.NewGuid(),
                    SpotifyArtistId = obj.Id,
                    Name = obj.Name
                };
                _db.Set<Artist>().Add(artist);
            }
            artists.Add(artist);
        }
        return artists;
    }
}
