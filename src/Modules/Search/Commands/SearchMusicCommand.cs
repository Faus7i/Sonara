using MediatR;
using MusicRec.Search.DTOs;

namespace MusicRec.Search.Commands;

/// <summary>
/// 统一音乐搜索 — 委托 Spotify API，结果自动落库
/// </summary>
/// <param name="Query">搜索关键词</param>
/// <param name="Type">搜索类型：track,artist,album（逗号分隔）</param>
/// <param name="Limit">每类结果数（最大 50）</param>
/// <param name="UserId">可选的用户 ID，用于记录搜索历史</param>
public record SearchMusicCommand(
    string Query,
    string Type = "track,artist,album",
    int Limit = 20,
    Guid? UserId = null
) : IRequest<SearchResultDto>;
