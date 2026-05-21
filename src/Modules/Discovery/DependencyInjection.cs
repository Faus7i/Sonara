using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;

namespace MusicRec.Discovery;

/// <summary>
/// Discovery 模块 DI 注册 — 探索推荐、冷启动推荐
/// </summary>
/// <remarks>
/// 本模块无自有实体/表，所有结果在内存中计算。
/// 跨模块只读访问 Catalog 和 AudioFeatures 的数据。
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddDiscoveryModule(this IServiceCollection services)
    {
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
