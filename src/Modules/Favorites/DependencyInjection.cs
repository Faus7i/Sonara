using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;

namespace MusicRec.Favorites;

/// <summary>
/// Favorites 模块 DI 注册 — 收藏系统的依赖注入配置
/// </summary>
/// <remarks>
/// 遵循标准模块注册模板：实体配置注册 → MediatR 扫描 → FluentValidation 扫描。
/// 不注册 IPipelineBehavior（由 Program.cs 全局统一注册，避免多次执行）。
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddFavoritesModule(this IServiceCollection services)
    {
        // 向共享 DbContext 注册本模块的 IEntityTypeConfiguration
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);
        // 扫描本程序集的 Command/Query/Handler
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        // 扫描本程序集的 FluentValidation 验证器
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
