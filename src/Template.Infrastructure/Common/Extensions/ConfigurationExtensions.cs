using Microsoft.Extensions.Configuration;

namespace Template.Infrastructure.Common.Extensions;

internal static class ConfigurationExtensions
{
    internal const string DefaultConnectionName = "DefaultConnection";

    internal static string GetRequiredConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionName}' was not found. Set it in appsettings.Development.json " +
                $"or via user-secrets: dotnet user-secrets set \"ConnectionStrings:{DefaultConnectionName}\" \"...\"");
        }

        return connectionString;
    }
}
