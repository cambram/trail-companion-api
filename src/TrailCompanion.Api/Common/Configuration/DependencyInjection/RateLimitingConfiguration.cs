using System.Threading.RateLimiting;

namespace TrailCompanion.Api.Common.Configuration.DependencyInjection;

internal static class RateLimitingConfiguration
{
    internal const string LoginPolicyName = "login";

    internal const int LoginPermitLimit = 5;

    internal static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginPolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LoginPermitLimit,
                    Window = TimeSpan.FromMinutes(1)
                }));
        });

        return services;
    }
}
