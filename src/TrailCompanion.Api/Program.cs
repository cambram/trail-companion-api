using TrailCompanion.Api.Common.Configuration.DependencyInjection;
using TrailCompanion.Api.Common.Configuration.Extensions;
using TrailCompanion.Api.Common.Middleware;
using TrailCompanion.Application.Common.Configuration.Extensions;
using TrailCompanion.Infrastructure.Common.Configuration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.AddApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            $"/openapi/{OpenApiConfiguration.DocumentName}.json",
            $"{OpenApiConfiguration.DocumentTitle} {OpenApiConfiguration.DocumentName}");
        options.RoutePrefix = OpenApiConfiguration.RoutePrefix;
        options.DocumentTitle = OpenApiConfiguration.DocumentTitle;
    });
}

app.UseCors(CorsConfiguration.SpaPolicyName);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapApiEndpoints();

app.Run();
