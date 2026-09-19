using TrailCompanion.Api.Common.Schemas;

namespace TrailCompanion.Api.Common.Configuration.DependencyInjection;

internal static class OpenApiConfiguration
{
    internal const string DocumentName = "v1";

    internal const string DocumentTitle = "TrailCompanion API";

    internal const string RoutePrefix = "swagger";

    internal static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizationRequirementOperationTransformer>();
        });

        return services;
    }
}
