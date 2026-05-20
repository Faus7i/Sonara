using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Abstractions;
using MusicRec.Identity.Services;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Identity;

/// <summary>
/// Identity 模块 DI 注册 — 集中管理本模块所有服务的依赖注入
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        // 注册实体配置到共享 DbContext（设计时与运行时均需调用）
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);

        // MediatR：自动扫描本程序集中的 Command/Query/Handler
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // FluentValidation：自动扫描验证器 + 插入 MediatR 管道自动验证
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // 密码哈希 — 单例，无状态
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        // JWT 服务 — Scoped，每个请求读取一次配置
        services.AddScoped<JwtService>();

        return services;
    }
}
