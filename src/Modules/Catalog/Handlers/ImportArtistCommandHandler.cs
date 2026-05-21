using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.Commands;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;
using MusicRec.Spotify;

namespace MusicRec.Catalog.Handlers;

/// <summary>
/// 从 Spotify 导入艺术家及其热门曲目
/// </summary>
/// <remarks>
/// Cache-Aside 模式：先查本地 DB，命中直接返回；
/// 未命中则通过 Spotify API 获取艺术家详情 + 热门曲目，一并落库。
/// Spotify 的 GetArtistTopTracks 最多返回 10 条（API 限制，非业务截断）。
/// </remarks>
public class ImportArtistCommandHandler : IRequestHandler<ImportArtistCommand, ArtistDto>
{
    private const int TopTracksLimit = 10; // Spotify API 上限，非业务截断

    private readonly MusicRecDbContext _db;
    private readonly ISpotifyClient _spotify;

    public ImportArtistCommandHandler(MusicRecDbContext db, ISpotifyClient spotify)
    {
        _db = db;
        _spotify = spotify;
    }

    public async Task<ArtistDto> Handle(ImportArtistCommand request, CancellationToken ct)
    {
        var existing = await _db.Set<Artist>()
            .FirstOrDefaultAsync(a => a.SpotifyArtistId == request.SpotifyArtistId, ct);

        if (existing is not null)
            return existing.Adapt<ArtistDto>();

        var artistObj = await _spotify.GetArtistAsync(request.SpotifyArtistId, ct);

        var artist = new Artist
        {
            Id = Guid.NewGuid(),
            SpotifyArtistId = artistObj.Id,
            Name = artistObj.Name,
            // Genres 存为逗号分隔字符串 — Artist 的 genre 通常只有 1-5 个，无需独立关联表
            Genres = artistObj.Genres.Count > 0 ? string.Join(",", artistObj.Genres) : null,
            ImageUrl = artistObj.Images.FirstOrDefault()?.Url,
            Popularity = artistObj.Popularity
        };
        _db.Set<Artist>().Add(artist);

        // 获取并导入热门曲目（含关联的专辑）
        var topTracks = await _spotify.GetArtistTopTracksAsync(request.SpotifyArtistId, ct: ct);
        foreach (var trackObj in topTracks.Take(TopTracksLimit))
        {
            // Spotify 本地文件或无专辑曲目的 Album 可能为 null，跳过此类曲目
            if (trackObj.Album is null) continue;

            var trackExists = await _db.Set<Track>()
                .AnyAsync(t => t.SpotifyTrackId == trackObj.Id, ct);
            if (trackExists) continue;

            var album = await _db.Set<Album>()
                .FirstOrDefaultAsync(a => a.SpotifyAlbumId == trackObj.Album.Id, ct);
            if (album is null)
            {
                album = new Album
                {
                    Id = Guid.NewGuid(),
                    SpotifyAlbumId = trackObj.Album.Id,
                    Name = trackObj.Album.Name,
                    ReleaseDate = trackObj.Album.ReleaseDate,
                    CoverImageUrl = trackObj.Album.Images.FirstOrDefault()?.Url,
                    AlbumType = trackObj.Album.AlbumType,
                    TotalTracks = trackObj.Album.TotalTracks
                };
                _db.Set<Album>().Add(album);
            }

            _db.Set<Track>().Add(new Track
            {
                Id = Guid.NewGuid(),
                SpotifyTrackId = trackObj.Id,
                Name = trackObj.Name,
                AlbumId = album.Id,
                DurationMs = trackObj.DurationMs,
                Popularity = trackObj.Popularity,
                ReleaseDate = trackObj.Album.ReleaseDate,
                CoverImageUrl = trackObj.Album.Images.FirstOrDefault()?.Url
            });
        }

        await _db.SaveChangesAsync(ct);
        return artist.Adapt<ArtistDto>();
    }
}
