using TrailCompanion.Api.Common.Configuration.DependencyInjection;

namespace TrailCompanion.Api.Common.Filters;

internal sealed class AllowedOriginFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var origin = context.HttpContext.Request.Headers.Origin.ToString();

        if (origin.Length == 0)
        {
            return next(context);
        }

        var allowed = CorsConfiguration.AllowedOrigins(
            context.HttpContext.RequestServices.GetRequiredService<IConfiguration>());

        if (!allowed.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Cross-site request rejected.");
        }

        return next(context);
    }
}
