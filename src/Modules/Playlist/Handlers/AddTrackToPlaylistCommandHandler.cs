using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MusicRec.Catalog.Entities;
using MusicRec.Playlists.Commands;
using MusicRec.Playlists.DTOs;
using MusicRec.Playlists.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 向歌单添加曲目处理器
/// </summary>
/// <remarks>
/// OrderIndex 自动计算为当前歌单最大 OrderIndex + 1（追加到末尾）。
/// 重复添加由数据库唯一索引 (PlaylistId, TrackId) 拦截，转为 ConflictException。
/// 返回完整的 PlaylistTrackItemDto 包含真实曲目信息，前端无需二次请求。
/// </remarks>
public class AddTrackToPlaylistCommandHandler : IRequestHandler<AddTrackToPlaylistCommand, PlaylistTrackItemDto>
{
    private readonly MusicRecDbContext _db;

    public AddTrackToPlaylistCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task<PlaylistTrackItemDto> Handle(AddTrackToPlaylistCommand request, CancellationToken ct)
    {
        // 第一步：验证歌单存在 + 所有权
        var playlist = await _db.Set<Playlist>()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, ct);

        if (playlist is null)
            throw new NotFoundException("歌单不存在");
        if (playlist.UserId != request.UserId)
            throw new UnauthorizedException("无权操作该歌单");

        // 第二步：验证曲目在本地库中存在
        var trackExists = await _db.Set<Track>().AnyAsync(t => t.Id == request.TrackId, ct);
        if (!trackExists)
            throw new NotFoundException("曲目不存在，请先从 Spotify 导入");

        // 第三步：计算 OrderIndex（追加到歌单末尾）
        var maxOrder = await _db.Set<PlaylistTrack>()
            .Where(pt => pt.PlaylistId == request.PlaylistId)
            .MaxAsync(pt => (int?)pt.OrderIndex, ct) ?? -1;

        var playlistTrack = new PlaylistTrack
        {
            Id = Guid.NewGuid(),
            PlaylistId = request.PlaylistId,
            TrackId = request.TrackId,
            OrderIndex = maxOrder + 1,
            AddedAt = DateTime.UtcNow
        };
        _db.Set<PlaylistTrack>().Add(playlistTrack);
        playlist.UpdatedAt = DateTime.UtcNow;

        // 第四步：写入 + 并发重复拦截
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            throw new ConflictException("该曲目已在歌单中");
        }

        // 第五步：查询曲目详情用于返回（避免前端拿到数据后还得再请求）
        var track = await _db.Set<Track>()
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .FirstOrDefaultAsync(t => t.Id == request.TrackId, ct);

        var artistsSummary = track?.TrackArtists?.Count > 0
            ? string.Join(", ", track.TrackArtists.Select(ta => ta.Artist.Name))
            : "未知艺术家";

        return new PlaylistTrackItemDto(
            Id: playlistTrack.Id,
            OrderIndex: playlistTrack.OrderIndex,
            AddedAt: playlistTrack.AddedAt,
            TrackId: request.TrackId,
            Name: track?.Name ?? "未知曲目",
            CoverImageUrl: track?.CoverImageUrl,
            DurationMs: track?.DurationMs ?? 0,
            ArtistsSummary: artistsSummary
        );
    }
}
