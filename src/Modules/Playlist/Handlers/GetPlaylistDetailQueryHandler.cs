using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.Entities;
using MusicRec.Playlists.DTOs;
using MusicRec.Playlists.Entities;
using MusicRec.Playlists.Queries;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 获取歌单详情处理器 — 包含完整曲目列表和曲目信息
/// </summary>
/// <remarks>
/// 权限规则：公开歌单（IsPublic=true）任何人可查看；
/// 私有歌单（IsPublic=false）仅所有者可查看。
/// 两阶段查询：先取歌单+曲目关联，再批量取曲目详情，避免 N+1 问题。
/// </remarks>
public class GetPlaylistDetailQueryHandler : IRequestHandler<GetPlaylistDetailQuery, PlaylistDetailDto>
{
    private readonly MusicRecDbContext _db;

    public GetPlaylistDetailQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<PlaylistDetailDto> Handle(GetPlaylistDetailQuery request, CancellationToken ct)
    {
        // 阶段一：查询歌单 + 关联曲目（按 OrderIndex 排序）
        var playlist = await _db.Set<Playlist>()
            .Include(p => p.PlaylistTracks.OrderBy(pt => pt.OrderIndex))
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, ct);

        if (playlist is null)
            throw new NotFoundException("歌单不存在");

        // 私有歌单权限检查：非所有者无权查看
        if (!playlist.IsPublic && playlist.UserId != request.CurrentUserId)
            throw new UnauthorizedException("无权访问该私有歌单");

        // 阶段二：批量获取曲目详情（单次查询替代 N 次查询）
        var trackIds = playlist.PlaylistTracks.Select(pt => pt.TrackId).ToList();
        var tracks = await _db.Set<Track>()
            .Where(t => trackIds.Contains(t.Id))
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .ToDictionaryAsync(t => t.Id, ct);

        // 阶段三：组装 DTO（展平 Track + Artist 信息）
        var trackItems = playlist.PlaylistTracks.Select(pt =>
        {
            tracks.TryGetValue(pt.TrackId, out var track);
            var artistsSummary = track?.TrackArtists?.Count > 0
                ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist.Name))
                : "未知艺术家";

            return new PlaylistTrackItemDto(
                Id: pt.Id,
                OrderIndex: pt.OrderIndex,
                AddedAt: pt.AddedAt,
                TrackId: pt.TrackId,
                Name: track?.Name ?? "未知曲目",
                CoverImageUrl: track?.CoverImageUrl,
                DurationMs: track?.DurationMs ?? 0,
                ArtistsSummary: artistsSummary
            );
        }).ToList();

        return new PlaylistDetailDto(
            Id: playlist.Id,
            Name: playlist.Name,
            Description: playlist.Description,
            CoverImageUrl: playlist.CoverImageUrl,
            IsPublic: playlist.IsPublic,
            Tracks: trackItems,
            CreatedAt: playlist.CreatedAt,
            UpdatedAt: playlist.UpdatedAt
        );
    }
}
