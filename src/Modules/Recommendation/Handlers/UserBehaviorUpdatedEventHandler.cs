using MediatR;
using Microsoft.Extensions.Logging;
using MusicRec.Contracts.Events;
using MusicRec.Shared.Caching;

namespace MusicRec.Recommendation.Handlers;

/// <summary>
/// 用户画像更新事件订阅 — 当 UserBehavior 模块刷新用户画像后触发
/// </summary>
/// <remarks>
/// 用户画像刷新后自动清除该用户的推荐缓存，确保下次请求获取基于最新画像的推荐结果。
/// 缓存失效使用前缀匹配，一次性清除 rec:user:{userId}:* 所有键（含不同 limit 的变体）。
/// </remarks>
public class UserBehaviorUpdatedEventHandler : INotificationHandler<UserBehaviorUpdatedEvent>
{
    private readonly ILogger<UserBehaviorUpdatedEventHandler> _logger;
    private readonly ICacheService _cache;

    public UserBehaviorUpdatedEventHandler(ILogger<UserBehaviorUpdatedEventHandler> logger, ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task Handle(UserBehaviorUpdatedEvent notification, CancellationToken ct)
    {
        var prefix = CacheKeys.UserRecommendationPrefix(notification.UserId);
        await _cache.RemoveByPrefixAsync(prefix, ct);

        _logger.LogInformation("用户 {UserId} 画像已更新，推荐缓存已失效（前缀={Prefix}）",
            notification.UserId, prefix);
    }
}
