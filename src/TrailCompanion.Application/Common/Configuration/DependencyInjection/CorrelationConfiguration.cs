using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Application.Common.Configuration.DependencyInjection;

internal static class CorrelationConfiguration
{
    internal static IServiceCollection AddCorrelation(this IServiceCollection services)
    {
        services.AddSingleton<CorrelationContext>();
        services.AddSingleton<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());

        return services;
    }
}
