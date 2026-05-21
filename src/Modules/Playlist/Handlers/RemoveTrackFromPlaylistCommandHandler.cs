using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Playlists.Commands;
using MusicRec.Playlists.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 从歌单移除曲目处理器 — 幂等设计
/// </summary>
public class RemoveTrackFromPlaylistCommandHandler : IRequestHandler<RemoveTrackFromPlaylistCommand>
{
    private readonly MusicRecDbContext _db;

    public RemoveTrackFromPlaylistCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task Handle(RemoveTrackFromPlaylistCommand request, CancellationToken ct)
    {
        // 验证歌单存在 + 所有权
        var playlist = await _db.Set<Playlist>()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, ct);

        if (playlist is null)
            throw new NotFoundException("歌单不存在");
        if (playlist.UserId != request.UserId)
            throw new UnauthorizedException("无权操作该歌单");

        var track = await _db.Set<PlaylistTrack>()
            .FirstOrDefaultAsync(pt => pt.PlaylistId == request.PlaylistId
                && pt.TrackId == request.TrackId, ct);

        if (track is null)
            return; // 幂等：曲目已不在歌单中，静默返回成功

        _db.Set<PlaylistTrack>().Remove(track);
        playlist.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
