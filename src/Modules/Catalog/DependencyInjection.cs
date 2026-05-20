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
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
