using MediatR;
using MusicRec.Discovery.DTOs;

namespace MusicRec.Discovery.Queries;

/// <summary>
/// 个性化探索推荐 — 基于用户画像做 70/20/10 探索分配
/// </summary>
public record GetDiscoveryQuery(Guid UserId, int Limit = 20)
    : IRequest<IReadOnlyList<DiscoveryResultDto>>;

/// <summary>
/// 冷启动推荐 — 面向新用户或未登录用户，基于热度 + 流派多样性
/// </summary>
public record GetColdStartQuery(Guid? UserId, int Limit = 20)
    : IRequest<IReadOnlyList<DiscoveryResultDto>>;
