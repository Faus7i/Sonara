using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicRec.AudioFeatures.Entities;
using MusicRec.Catalog.DTOs;
using MusicRec.Catalog.Entities;
using MusicRec.Catalog.Queries;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Catalog.Handlers;

/// <summary>
/// 曲目详情查询处理器 — 两阶段加载：曲目主体 + 音频特征（可选）
/// </summary>
/// <remarks>
/// 阶段 1：查询 Track（含 Album、TrackArtists、Artist），Mapster 映射为 TrackDto
/// 阶段 2：查询 TrackAudioFeature（通过 SpotifyTrackId 关联），存在则用 with 表达式注入
///
/// 两个查询为串行——阶段 2 依赖阶段 1 获取的 SpotifyTrackId。
/// AudioFeatures 查询失败不阻塞主流程：TrackAudioFeature 表可能无该曲目数据（未导入音频特征），
/// 此时 AudioFeatures 保持 null，前端隐去音频特征分析区块。
/// </remarks>
public class GetTrackQueryHandler : IRequestHandler<GetTrackQuery, TrackDto>
{
    private readonly MusicRecDbContext _db;

    public GetTrackQueryHandler(MusicRecDbContext db) => _db = db;

    public async Task<TrackDto> Handle(GetTrackQuery request, CancellationToken ct)
    {
        // ── 阶段 1：加载曲目主体（含专辑和艺术家导航属性）─────────
        var track = await _db.Set<Track>()
            .Include(t => t.Album)
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TrackId, ct);

        if (track is null)
            throw new NotFoundException("曲目不存在");

        var dto = track.Adapt<TrackDto>();

        // ── 阶段 2：加载音频特征（通过 SpotifyTrackId 关联）─────
        // TrackAudioFeature 是独立实体，无 FK 约束，通过业务键 SpotifyTrackId 关联
        var audioFeature = await _db.Set<TrackAudioFeature>()
            .AsNoTracking()
            .FirstOrDefaultAsync(af => af.SpotifyTrackId == track.SpotifyTrackId, ct);

        // with 表达式创建新 TrackDto 实例，仅 AudioFeatures 不同，其余字段复用
        // audioFeature 为 null 时 Adapt 返回 null，AudioFeatures 保持默认值
        return dto with { AudioFeatures = audioFeature?.Adapt<AudioFeaturesBriefDto>() };
    }
}
