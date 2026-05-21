using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace MusicRec.Player;

/// <summary>
/// Player 模块 DI 注册 — 纯 API 适配层，无本地数据库实体
/// </summary>
/// <remarks>
/// 不调用 EntityConfigurationRegistry.Register()——Player 模块无实体/EF 配置。
/// 所有功能通过 ISpotifyClient 代理到 Spotify Web API 的播放控制端点。
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddPlayerModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
