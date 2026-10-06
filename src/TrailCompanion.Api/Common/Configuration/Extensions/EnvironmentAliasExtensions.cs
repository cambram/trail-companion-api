using System.Web;
using Npgsql;

namespace TrailCompanion.Api.Common.Configuration.Extensions;

/// <summary>
/// Maps the flat SCREAMING_SNAKE environment variables used by hosting platforms
/// (Render, Neon) onto the configuration keys the app binds to, so deployments only
/// have to deal with one naming convention.
/// </summary>
internal static class EnvironmentAliasExtensions
{
    private const int DefaultPostgresPort = 5432;

    internal static WebApplicationBuilder AddEnvironmentAliases(this WebApplicationBuilder builder)
    {
        var aliases = new Dictionary<string, string?>();

        // Neon hands out a pooled and a direct URL. Prefer the pooled one.
        var databaseUrl = FirstSet("DATABASE_URL_POOLED", "DATABASE_URL");

        if (databaseUrl is not null)
        {
            aliases["ConnectionStrings:DefaultConnection"] = ToNpgsqlConnectionString(databaseUrl);
        }

        Alias(aliases, "JWT_KEY", "Jwt:Key");
        Alias(aliases, "JWT_ISSUER", "Jwt:Issuer");
        Alias(aliases, "JWT_AUDIENCE", "Jwt:Audience");
        Alias(aliases, "JWT_ACCESS_TOKEN_LIFETIME_MINUTES", "Jwt:AccessTokenLifetimeMinutes");
        Alias(aliases, "FEATURE_REGISTRATION_ENABLED", "FeatureManagement:RegistrationEnabled");
        Alias(aliases, "FEATURE_BREACHED_PASSWORD_CHECK", "FeatureManagement:BreachedPasswordCheck");

        var allowedOrigins = FirstSet("CORS_ALLOWED_ORIGINS");

        if (allowedOrigins is not null)
        {
            var origins = allowedOrigins.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (var i = 0; i < origins.Length; i++)
            {
                aliases[$"Cors:AllowedOrigins:{i}"] = origins[i];
            }
        }

        if (aliases.Count > 0)
        {
            builder.Configuration.AddInMemoryCollection(aliases);
        }

        return builder;
    }

    /// <summary>
    /// Npgsql does not accept URI-style connection strings, which is the only format
    /// Neon exposes. Key=value strings are passed through untouched.
    /// </summary>
    internal static string ToNpgsqlConnectionString(string databaseUrl)
    {
        if (!databaseUrl.Contains("://", StringComparison.Ordinal))
        {
            return databaseUrl;
        }

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var query = HttpUtility.ParseQueryString(uri.Query);

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPostgresPort,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = ParseSslMode(query["sslmode"]),
            // Neon's pooled endpoint is PgBouncer in transaction mode, which rejects DISCARD ALL.
            NoResetOnClose = true
        }.ConnectionString;
    }

    /// <summary>
    /// libpq spells the stricter modes with a hyphen ("verify-full"); Npgsql's enum does not.
    /// Anything unrecognised falls back to the mode every managed Postgres host expects.
    /// </summary>
    private static SslMode ParseSslMode(string? sslMode) =>
        Enum.TryParse<SslMode>(sslMode?.Replace("-", string.Empty), ignoreCase: true, out var parsed)
            ? parsed
            : SslMode.Require;

    private static void Alias(Dictionary<string, string?> aliases, string variable, string configurationKey)
    {
        var value = FirstSet(variable);

        if (value is not null)
        {
            aliases[configurationKey] = value;
        }
    }

    private static string? FirstSet(params string[] variables) => variables
        .Select(Environment.GetEnvironmentVariable)
        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
