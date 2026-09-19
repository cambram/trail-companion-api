using Template.Infrastructure.Common.Configuration.DependencyInjection;
using Template.Infrastructure.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Template.Infrastructure.Common.Configuration.Extensions;

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

        return services;
    }
}
