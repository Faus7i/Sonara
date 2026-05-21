using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.Playlists.Commands;
using MusicRec.Playlists.DTOs;
using MusicRec.Playlists.Entities;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Playlists.Handlers;

/// <summary>
/// 编辑歌单处理器 — 支持部分更新（仅修改非 null 字段）
/// </summary>
/// <remarks>
/// 所有权验证必不可少：UserId 必须匹配歌单创建者，防止用户修改他人歌单。
/// 更新后重新计算 TrackCount 以确保返回数据准确。
/// </remarks>
public class UpdatePlaylistCommandHandler : IRequestHandler<UpdatePlaylistCommand, PlaylistDto>
{
    private readonly MusicRecDbContext _db;

    public UpdatePlaylistCommandHandler(MusicRecDbContext db) => _db = db;

    public async Task<PlaylistDto> Handle(UpdatePlaylistCommand request, CancellationToken ct)
    {
        var playlist = await _db.Set<Playlist>()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, ct);

        if (playlist is null)
            throw new NotFoundException("歌单不存在");

        // 所有权验证：仅歌单创建者可编辑
        if (playlist.UserId != request.UserId)
            throw new UnauthorizedException("无权编辑该歌单");

        // 部分更新：仅修改客户端明确传入的字段
        if (request.Name is not null) playlist.Name = request.Name;
        if (request.Description is not null) playlist.Description = request.Description;
        if (request.CoverImageUrl is not null) playlist.CoverImageUrl = request.CoverImageUrl;
        if (request.IsPublic.HasValue) playlist.IsPublic = request.IsPublic.Value;
        playlist.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var trackCount = await _db.Set<PlaylistTrack>()
            .CountAsync(pt => pt.PlaylistId == playlist.Id, ct);

        return new PlaylistDto(playlist.Id, playlist.Name, playlist.Description,
            playlist.CoverImageUrl, playlist.IsPublic, trackCount,
            playlist.CreatedAt, playlist.UpdatedAt);
    }
}
