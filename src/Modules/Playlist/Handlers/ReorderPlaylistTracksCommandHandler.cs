using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Playlists.Commands;
using MusicRec.Playlists.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 重新排序歌单曲目处理器 — 按传入的 TrackId 列表重新分配 OrderIndex
/// </summary>
/// <remarks>
/// 验证传入的所有 TrackId 都必须属于该歌单，防止恶意注入不属于本歌单的曲目。
/// 所有 OrderIndex 在单次 SaveChangesAsync 中原子更新。
/// </remarks>
public class ReorderPlaylistTracksCommandHandler : IRequestHandler<ReorderPlaylistTracksCommand>
{
    private readonly MusicRecDbContext _db;

    public ReorderPlaylistTracksCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task Handle(ReorderPlaylistTracksCommand request, CancellationToken ct)
    {
        // 验证歌单存在 + 所有权
        var playlist = await _db.Set<Playlist>()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, ct);

        if (playlist is null)
            throw new NotFoundException("歌单不存在");
        if (playlist.UserId != request.UserId)
            throw new UnauthorizedException("无权操作该歌单");

        // 加载当前歌单中的所有曲目
        var tracks = await _db.Set<PlaylistTrack>()
            .Where(pt => pt.PlaylistId == request.PlaylistId)
            .ToListAsync(ct);

        // 安全验证：传入的 TrackId 必须全部属于该歌单
        var existingTrackIds = tracks.Select(t => t.TrackId).ToHashSet();
        foreach (var trackId in request.TrackIds)
        {
            if (!existingTrackIds.Contains(trackId))
                throw new ConflictException($"曲目 {trackId} 不在该歌单中");
        }

        // 按新顺序重新分配 OrderIndex（0, 1, 2, ...）
        for (var i = 0; i < request.TrackIds.Count; i++)
        {
            var track = tracks.First(t => t.TrackId == request.TrackIds[i]);
            track.OrderIndex = i;
        }

        playlist.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
