using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Infrastructure;
using MusicRec.Search.Commands;
using MusicRec.Search.DTOs;
using MusicRec.Search.Entities;
using MusicRec.Spotify;
using MusicRec.Spotify.Models;

namespace MusicRec.Search.Handlers;

/// <summary>
/// 统一搜索 Handler：调用 Spotify 搜索 API，结果直接映射为 DTO 返回
/// </summary>
/// <remarks>
/// 搜索结果从 Spotify 实时获取，不做本地缓存（导入操作由 Catalog 模块的 ImportTrack/ImportArtist 负责）。
/// 仅搜索历史记录写入 UserSearchHistory 表。
/// </remarks>
public class SearchMusicCommandHandler : IRequestHandler<SearchMusicCommand, SearchResultDto>
{
    private readonly MusicRecDbContext _db;
    private readonly ISpotifyClient _spotify;

    public SearchMusicCommandHandler(MusicRecDbContext db, ISpotifyClient spotify)
    {
        _db = db;
        _spotify = spotify;
    }

    public async Task<SearchResultDto> Handle(SearchMusicCommand request, CancellationToken ct)
    {
        // 记录搜索历史
        if (request.UserId.HasValue)
        {
            _db.Set<SearchHistory>().Add(new SearchHistory
            {
                UserId = request.UserId.Value,
                Keyword = request.Query
            });
            await _db.SaveChangesAsync(ct);
        }

        // 调用 Spotify 搜索
        var response = await _spotify.SearchAsync(request.Query, request.Type, request.Limit, ct: ct);

        return new SearchResultDto(
            Tracks: MapTracks(response.Tracks?.Items ?? new List<TrackObject>()),
            Artists: MapArtists(response.Artists?.Items ?? new List<ArtistObject>()),
            Albums: MapAlbums(response.Albums?.Items ?? new List<SimplifiedAlbumObject>())
        );
    }

    private static IReadOnlyList<SearchTrackDto> MapTracks(List<TrackObject> tracks)
    {
        return tracks.Select(t => new SearchTrackDto(
            SpotifyTrackId: t.Id,
            Name: t.Name,
            DurationMs: t.DurationMs,
            Popularity: t.Popularity,
            CoverImageUrl: t.Album.Images.FirstOrDefault()?.Url,
            AlbumName: t.Album.Name,
            ArtistsSummary: string.Join(", ", t.Artists.Select(a => a.Name))
        )).ToList();
    }

    private static IReadOnlyList<SearchArtistDto> MapArtists(List<ArtistObject> artists)
    {
        return artists.Select(a => new SearchArtistDto(
            SpotifyArtistId: a.Id,
            Name: a.Name,
            Genres: a.Genres.Count > 0 ? string.Join(",", a.Genres) : null,
            ImageUrl: a.Images.FirstOrDefault()?.Url,
            Popularity: a.Popularity
        )).ToList();
    }

    private static IReadOnlyList<SearchAlbumDto> MapAlbums(List<SimplifiedAlbumObject> albums)
    {
        return albums.Select(a => new SearchAlbumDto(
            SpotifyAlbumId: a.Id,
            Name: a.Name,
            ReleaseDate: a.ReleaseDate,
            CoverImageUrl: a.Images.FirstOrDefault()?.Url,
            AlbumType: a.AlbumType,
            ArtistsSummary: string.Join(", ", a.Artists.Select(ar => ar.Name))
        )).ToList();
    }
}
