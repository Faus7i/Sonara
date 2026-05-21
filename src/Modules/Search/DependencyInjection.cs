using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.Search;

public static class DependencyInjection
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services)
    {
        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
