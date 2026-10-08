using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class GetRacesEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public GetRacesEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetRaces_WithoutAToken_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Route(ApiEndpoints.Users.GetRaces));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRaces_ReturnsOnlyTheCallersRaces()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        await owner.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", 90));
        await other.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("Not mine", null));

        var response = await owner.GetAsync(Route(ApiEndpoints.Users.GetRaces));

        response.EnsureSuccessStatusCode();
        var races = await response.Content.ReadFromJsonAsync<List<RaceDto>>(TestJson.Options);
        var race = Assert.Single(races!);
        Assert.Equal("UTMB", race.Name);
        Assert.Equal(90, race.Target);
    }

    private static async Task SignIn(HttpClient client)
    {
        var username = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username, Password));
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));

    private static string Route(string route) => $"/{route}";
}
