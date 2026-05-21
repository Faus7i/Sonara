using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Playlists.Commands;
using MusicRec.Playlists.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 删除歌单处理器 — Cascade 自动清理关联的 PlaylistTracks
/// </summary>
public class DeletePlaylistCommandHandler : IRequestHandler<DeletePlaylistCommand>
{
    private readonly MusicRecDbContext _db;

    public DeletePlaylistCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task Handle(DeletePlaylistCommand request, CancellationToken ct)
    {
        var playlist = await _db.Set<Playlist>()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, ct);

        if (playlist is null)
            throw new NotFoundException("歌单不存在");

        // 所有权验证：仅歌单创建者可删除
        if (playlist.UserId != request.UserId)
            throw new UnauthorizedException("无权删除该歌单");

        // EF Cascade 自动删除所有关联的 PlaylistTrack（见 PlaylistConfiguration）
        _db.Set<Playlist>().Remove(playlist);
        await _db.SaveChangesAsync(ct);
    }
}
