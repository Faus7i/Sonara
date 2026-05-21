using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Catalog;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        // 配置 Track → TrackDto 的 Mapster 映射（TrackArtists → Artists 展平）
        CatalogMapping.Configure();

        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
