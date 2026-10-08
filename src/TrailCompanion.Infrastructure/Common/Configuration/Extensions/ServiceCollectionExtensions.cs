using TrailCompanion.Infrastructure.Common.Configuration.DependencyInjection;
using TrailCompanion.Infrastructure.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Infrastructure.Common.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString();

        services.AddDatabase(connectionString);
        services.AddRepositories();
        services.AddAuthenticationServices(configuration);
        services.AddAppTimeZone(configuration);

        return services;
    }
}
