using TrailCompanion.Api.Common.Middleware;

namespace TrailCompanion.Api.Common.Configuration.DependencyInjection;

internal static class CorsConfiguration
{
    internal const string SpaPolicyName = "SpaCors";

    internal static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(SpaPolicyName, policy => policy
                .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Authorization", "Content-Type", CorrelationIdMiddleware.HeaderName)
                .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)
                .AllowCredentials());
        });

        return services;
    }
}
