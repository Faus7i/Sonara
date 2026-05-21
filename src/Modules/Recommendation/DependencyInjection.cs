using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;

namespace MusicRec.Recommendation;

/// <summary>
/// Recommendation 模块 DI 注册 — 个性化推荐算法、种子数据生成、用户画像事件订阅
/// </summary>
/// <remarks>
/// 本模块无自有实体/表，所有推荐结果在内存中计算。
/// 跨模块只读访问 Catalog/AudioFeatures/Favorites/UserBehavior 的数据。
/// EntityConfigurationRegistry.Register 调用无害（程序集无 IEntityTypeConfiguration 时为空操作）。
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddRecommendationModule(this IServiceCollection services)
    {
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
