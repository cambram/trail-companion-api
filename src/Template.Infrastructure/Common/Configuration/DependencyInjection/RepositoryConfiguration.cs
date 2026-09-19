using Template.Domain.Modules.Users.Repositories;
using Template.Infrastructure.Repositories.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Template.Infrastructure.Common.Configuration.DependencyInjection;

internal static class RepositoryConfiguration
{
    internal static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAppUserRepository, AppUserRepository>();

        return services;
    }
}
