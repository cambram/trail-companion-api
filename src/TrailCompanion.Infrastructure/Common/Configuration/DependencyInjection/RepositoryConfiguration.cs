using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Repositories.Users;
using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Infrastructure.Common.Configuration.DependencyInjection;

internal static class RepositoryConfiguration
{
    internal static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IRacesRepository, RacesRepository>();

        return services;
    }
}
