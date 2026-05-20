using MediatR;
using MusicRec.Identity.DTOs;

namespace MusicRec.Identity.Queries;

/// <summary>
/// 获取用户资料查询
/// </summary>
public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;
