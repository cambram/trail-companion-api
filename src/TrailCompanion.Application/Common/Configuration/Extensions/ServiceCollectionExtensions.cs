using TrailCompanion.Application.Common.Configuration.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Application.Common.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddCorrelation();
        services.AddFeatureFlags();
        services.AddLoginThrottle();

        return services;
    }
}
