using System.Net;
using System.Text.RegularExpressions;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class EndpointAuthorizationTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly string[] AnonymousRoutes =
    [
        "health",
        "openapi/{documentName}.json",
        ApiEndpoints.Users.RegisterUser,
        ApiEndpoints.Users.LoginUser,
        ApiEndpoints.Users.RefreshToken,
        ApiEndpoints.Users.Logout
    ];

    private readonly PostgresDatabaseFixture _fixture;

    public EndpointAuthorizationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EveryEndpoint_IsOnTheAnonymousListOrReturns401WithoutAToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var pattern = endpoint.RoutePattern.RawText!.TrimStart('/');
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"];

            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                Assert.Contains(pattern, AnonymousRoutes);
                continue;
            }

            foreach (var method in methods)
            {
                var path = Regex.Replace(pattern, "{[^}]+}", Guid.NewGuid().ToString());
                using var request = new HttpRequestMessage(new HttpMethod(method), $"/{path}");
                var response = await client.SendAsync(request);

                Assert.True(
                    response.StatusCode == HttpStatusCode.Unauthorized,
                    $"{method} /{pattern} returned {(int)response.StatusCode} without a token");
            }
        }
    }

    [Fact]
    public async Task Health_StaysAnonymous()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));
}
