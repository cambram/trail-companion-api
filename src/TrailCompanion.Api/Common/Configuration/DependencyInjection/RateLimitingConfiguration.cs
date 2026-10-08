using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TrailCompanion.Api.Common.Configuration.DependencyInjection;

internal static class RateLimitingConfiguration
{
    internal const string LoginPolicyName = "login";
    internal const string RefreshPolicyName = "refresh";
    internal const string AccountPolicyName = "account";

    internal const string RegisterPolicyName = "register";
    internal const int RegisterPermitLimit = 10;
    internal const string JoinPolicyName = "join";
    internal const int JoinPermitLimit = 10;
    internal const int PasswordHashingConcurrency = 2;
    internal const int PasswordHashingQueueLimit = 10;

    internal sealed record PasswordHashingEndpoint;

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
            AddPerIpPolicy(options, RegisterPolicyName, RegisterPermitLimit);
            AddPerIpPolicy(options, JoinPolicyName, JoinPermitLimit);
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.GetEndpoint()?.Metadata.GetMetadata<PasswordHashingEndpoint>() is null
                    ? RateLimitPartition.GetNoLimiter(string.Empty)
                    : RateLimitPartition.GetConcurrencyLimiter(
                        nameof(PasswordHashingEndpoint),
                        _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = PasswordHashingConcurrency,
                            QueueLimit = PasswordHashingQueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));
        });

        return services;
    }

    private static void AddPerIpPolicy(RateLimiterOptions options, string policyName, int permitLimit) =>
        options.AddPolicy(policyName, context => RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(context.Connection.RemoteIpAddress),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1)
            }));

    internal static string PartitionKey(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4().ToString();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);

        return $"{new IPAddress(bytes)}/64";
    }
}
