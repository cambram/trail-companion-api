using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TrailCompanion.Api.Common.Configuration.DependencyInjection;

internal static class RateLimitingConfiguration
{
    internal const string LoginPolicyName = "login";
    internal const string RefreshPolicyName = "refresh";
    internal const string AccountPolicyName = "account";

    internal const int LoginPermitLimit = 5;
    internal const int RefreshPermitLimit = 30;
    internal const int AccountPermitLimit = 5;

    internal static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddPerIpPolicy(options, LoginPolicyName, LoginPermitLimit);
            AddPerIpPolicy(options, RefreshPolicyName, RefreshPermitLimit);
            AddPerIpPolicy(options, AccountPolicyName, AccountPermitLimit);
        });

        return services;
    }

    private static void AddPerIpPolicy(RateLimiterOptions options, string policyName, int permitLimit) =>
        options.AddPolicy(policyName, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1)
            }));
}
