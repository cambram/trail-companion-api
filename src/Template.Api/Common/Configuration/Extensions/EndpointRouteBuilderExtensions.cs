using System.Reflection;

namespace Template.Api.Common.Configuration.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    internal static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var mapMethods = typeof(IApiAssemblyMarker).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => type.GetMethod(
                nameof(IEndpoint.MapEndpoint),
                BindingFlags.Public | BindingFlags.Static))
            .OfType<MethodInfo>();

        foreach (var mapMethod in mapMethods)
        {
            mapMethod.Invoke(null, [app]);
        }

        return app;
    }
}
