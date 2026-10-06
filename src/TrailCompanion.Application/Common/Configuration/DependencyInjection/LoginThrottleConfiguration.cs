using TrailCompanion.Application.Modules.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TrailCompanion.Application.Common.Configuration.DependencyInjection;

internal static class LoginThrottleConfiguration
{
    internal static IServiceCollection AddLoginThrottle(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LoginThrottle>();

        return services;
    }
}
