using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Playlists.DTOs;
using MusicRec.Playlists.Entities;
using MusicRec.Playlists.Queries;
using MusicRec.Infrastructure;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 获取用户歌单列表处理器 — 按更新时间降序，包含曲目数量
/// </summary>
/// <remarks>
/// 使用 EF Core 投影查询（Select）直接在数据库端完成 TrackCount 聚合，
/// 避免加载完整 PlaylistTracks 集合到内存后再计数。
/// </remarks>
public class GetUserPlaylistsQueryHandler : IRequestHandler<GetUserPlaylistsQuery, IReadOnlyList<PlaylistDto>>
{
    private readonly MusicRecDbContext _db;

    public GetUserPlaylistsQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlaylistDto>> Handle(GetUserPlaylistsQuery request, CancellationToken ct)
    {
        var playlists = await _db.Set<Playlist>()
            .Where(p => p.UserId == request.UserId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new PlaylistDto(
                p.Id, p.Name, p.Description, p.CoverImageUrl, p.IsPublic,
                p.PlaylistTracks.Count,
                p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);

        return playlists;
    }
}
