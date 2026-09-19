using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class DeleteRaceEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public DeleteRaceEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteRace_WithoutAToken_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync(DeleteRoute(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRace_OwnRace_Returns204AndRemovesIt()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client, "UTMB");

        var response = await client.DeleteAsync(DeleteRoute(race.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = await client.GetFromJsonAsync<List<RaceDto>>(Route(ApiEndpoints.Users.GetRaces));
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task DeleteRace_AnotherUsersRace_Returns404AndKeepsIt()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await CreateRace(owner, "UTMB");

        var response = await other.DeleteAsync(DeleteRoute(race.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var remaining = await owner.GetFromJsonAsync<List<RaceDto>>(Route(ApiEndpoints.Users.GetRaces));
        Assert.Single(remaining!);
    }

    [Fact]
    public async Task DeleteRace_UnknownId_Returns404()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);

        var response = await client.DeleteAsync(DeleteRoute(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<RaceDto> CreateRace(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest(name, null));
        return (await response.Content.ReadFromJsonAsync<RaceDto>())!;
    }

    private static async Task SignIn(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(email, Password));
        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(email, Password));
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));

    private static string DeleteRoute(Guid id) => $"/{ApiEndpoints.Users.DeleteRace.Replace("{id:guid}", id.ToString())}";

    private static string Route(string route) => $"/{route}";
}
