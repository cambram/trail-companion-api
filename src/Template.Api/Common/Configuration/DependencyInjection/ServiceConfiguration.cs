using Template.Api.Common.Services;

namespace Template.Api.Common.Configuration.DependencyInjection;

internal static class ServiceConfiguration
{
    internal static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
