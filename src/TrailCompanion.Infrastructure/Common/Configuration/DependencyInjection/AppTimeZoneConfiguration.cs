using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Infrastructure.Common.Configuration.DependencyInjection;

internal static class AppTimeZoneConfiguration
{
    internal static IServiceCollection AddAppTimeZone(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(AppTimeZone.Resolve(configuration[AppTimeZone.ConfigurationKey]));

        return services;
    }
}
