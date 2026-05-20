using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.Infrastructure;
using MusicRec.Shared;

namespace MusicRec.AudioFeatures;

public static class DependencyInjection
{
    public static IServiceCollection AddAudioFeaturesModule(this IServiceCollection services)
    {
        // 配置 Mapster 映射（AudioFeaturesObject → TrackAudioFeature），避免 Handler 中逐字段赋值
        AudioFeatureMapping.Configure();

        EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
