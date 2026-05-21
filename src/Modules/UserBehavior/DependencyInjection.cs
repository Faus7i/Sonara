using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;

namespace MusicRec.UserBehavior;

/// <summary>
/// UserBehavior 模块 DI 注册 — 用户行为追踪、行为事件采集、偏好画像系统
/// </summary>
/// <remarks>
/// 遵循标准模块注册模板：
///   1. EntityConfigurationRegistry.Register — 注册实体 EF 配置到共享 DbContext
///   2. AddMediatR — 扫描本程序集的 Command/Query Handler
///   3. AddValidatorsFromAssembly — 扫描本程序集的 FluentValidation Validator
///
/// 不注册 IPipelineBehavior（由 Program.cs 全局统一注册 ValidationBehavior，
/// 避免验证管道执行 N 次）。
///
/// 本模块涉及 3 张表：
///   UserPlayHistory — 播放记录
///   UserBehaviorEvents — 通用行为事件
///   UserProfiles — 用户偏好画像（UserId 主键，一对一 User）
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddUserBehaviorModule(this IServiceCollection services)
    {
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
