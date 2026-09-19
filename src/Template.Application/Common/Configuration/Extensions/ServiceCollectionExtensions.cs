using Template.Application.Common.Configuration.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Template.Application.Common.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddCorrelation();

        return services;
    }
}
