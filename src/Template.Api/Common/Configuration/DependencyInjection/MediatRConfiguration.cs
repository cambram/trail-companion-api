using Template.Application;

namespace Template.Api.Common.Configuration.DependencyInjection;

internal static class MediatRConfiguration
{
    internal static IServiceCollection AddApiMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IApiAssemblyMarker).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(IApplicationAssemblyMarker).Assembly);
        });

        return services;
    }
}
