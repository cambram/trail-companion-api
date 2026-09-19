using TrailCompanion.Api.Common.Configuration.DependencyInjection;

namespace TrailCompanion.Api.Common.Configuration.Extensions;

internal static class WebApplicationBuilderExtensions
{
    internal static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddApiAuthentication();
        builder.Services.AddApiRateLimiting();
        builder.Services.AddApiCors(builder.Configuration);
        builder.Services.AddApiServices();
        builder.Services.AddApiOpenApi();
        builder.Services.AddApiMediatR();

        return builder;
    }
}
