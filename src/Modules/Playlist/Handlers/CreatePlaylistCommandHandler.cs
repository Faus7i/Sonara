using MediatR;
using MusicRec.Playlists.Commands;
using MusicRec.Playlists.DTOs;
using MusicRec.Playlists.Entities;
using MusicRec.Infrastructure;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 创建歌单处理器 — 新建空歌单，TrackCount 初始为 0
/// </summary>
public class CreatePlaylistCommandHandler : IRequestHandler<CreatePlaylistCommand, PlaylistDto>
{
    private readonly MusicRecDbContext _db;

    public CreatePlaylistCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task<PlaylistDto> Handle(CreatePlaylistCommand request, CancellationToken ct)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Playlist>().Add(playlist);
        await _db.SaveChangesAsync(ct);

        // 新歌单无曲目，TrackCount = 0
        return new PlaylistDto(playlist.Id, playlist.Name, playlist.Description,
            playlist.CoverImageUrl, playlist.IsPublic, TrackCount: 0,
            playlist.CreatedAt, playlist.UpdatedAt);
    }
}
