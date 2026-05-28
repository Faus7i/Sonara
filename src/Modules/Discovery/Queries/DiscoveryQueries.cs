using MediatR;
using MusicRec.Discovery.DTOs;

namespace MusicRec.Discovery.Queries;

/// <summary>
/// Spotify 实时探索推荐 — 随机选取种子流派，调用 Spotify 推荐 API，
/// Cache-Aside 自动导入曲目到本地库，返回带本地 ID 的结果
/// </summary>
public record GetSpotifyExploreQuery(Guid? UserId, int Limit = 20)
    : IRequest<IReadOnlyList<DiscoveryResultDto>>;

/// <summary>
/// 个性化探索推荐 — 基于用户画像做 70/20/10 探索分配（保留用于本地数据场景）
/// </summary>
public record GetDiscoveryQuery(Guid UserId, int Limit = 20)
    : IRequest<IReadOnlyList<DiscoveryResultDto>>;

/// <summary>
/// 冷启动推荐 — 面向新用户或未登录用户，基于热度 + 流派多样性（保留用于本地数据场景）
/// </summary>
public record GetColdStartQuery(Guid? UserId, int Limit = 20, bool ForceRefresh = false)
    : IRequest<IReadOnlyList<DiscoveryResultDto>>;
