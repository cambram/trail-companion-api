using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace TrailCompanion.Application.Common.Configuration.DependencyInjection;

internal static class FeatureManagementConfiguration
{
    internal static IServiceCollection AddFeatureFlags(this IServiceCollection services)
    {
        services.AddFeatureManagement();

        return services;
    }
}
