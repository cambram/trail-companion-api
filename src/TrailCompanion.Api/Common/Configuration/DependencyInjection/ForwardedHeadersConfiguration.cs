using Microsoft.AspNetCore.HttpOverrides;

namespace TrailCompanion.Api.Common.Configuration.DependencyInjection;

internal static class ForwardedHeadersConfiguration
{
    internal static IServiceCollection AddApiForwardedHeaders(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
}
