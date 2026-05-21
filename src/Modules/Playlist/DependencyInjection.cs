using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;

namespace MusicRec.Playlists;

/// <summary>
/// Playlist 模块 DI 注册 — 歌单系统的依赖注入配置
/// </summary>
/// <remarks>
/// 注意：命名空间使用复数 MusicRec.Playlists 以避免与实体类 Playlist 冲突。
/// 其他模块引用时使用 using MusicRec.Playlists; 而非 MusicRec.Playlist;
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddPlaylistModule(this IServiceCollection services)
    {
        // 向共享 DbContext 注册本模块的 IEntityTypeConfiguration
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
