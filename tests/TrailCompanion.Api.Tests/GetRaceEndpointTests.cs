using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class GetRaceEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public GetRaceEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetRace_WithoutAToken_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(GetRoute(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRace_ReturnsSegmentsAndItemsNestedAndOrdered()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await Post<RaceDto>(client, Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", 90));
        var first = await Post<SegmentDto>(client, SegmentsRoute(race.Id), new SegmentRequest("Start to Aid 1", 90, 11, "Easy"));
        var second = await Post<SegmentDto>(client, SegmentsRoute(race.Id), new SegmentRequest("Aid 1 to Aid 2", 60, null, null));
        await Post<ItemDto>(client, ItemsRoute(race.Id, first.Id), new ItemRequest("Gel", 40));
        await Post<ItemDto>(client, ItemsRoute(race.Id, first.Id), new ItemRequest("Chew", 30));
        await Post<ItemDto>(client, ItemsRoute(race.Id, second.Id), new ItemRequest("Banana", 25));

        var response = await client.GetAsync(GetRoute(race.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<RaceDetailDto>();
        Assert.Equal("UTMB", detail!.Name);
        Assert.Equal(90, detail.Target);
        Assert.Collection(detail.Segments,
            s =>
            {
                Assert.Equal("Start to Aid 1", s.Name);
                Assert.Equal(11, s.Distance);
                Assert.Equal(["Gel", "Chew"], s.Items.Select(i => i.Name));
            },
            s =>
            {
                Assert.Equal("Aid 1 to Aid 2", s.Name);
                Assert.Equal(["Banana"], s.Items.Select(i => i.Name));
            });
    }

    [Fact]
    public async Task GetRace_WithNoSegments_ReturnsEmptyList()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await Post<RaceDto>(client, Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", null));

        var detail = await client.GetFromJsonAsync<RaceDetailDto>(GetRoute(race.Id));

        Assert.Empty(detail!.Segments);
    }

    [Fact]
    public async Task GetRace_AnotherUsersRaceOrUnknownId_Returns404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await Post<RaceDto>(owner, Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", null));

        var foreign = await other.GetAsync(GetRoute(race.Id));
        var unknown = await owner.GetAsync(GetRoute(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    private static async Task<T> Post<T>(HttpClient client, string route, object body)
    {
        var response = await client.PostAsJsonAsync(route, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
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

    private static string GetRoute(Guid id) =>
        Route(ApiEndpoints.Users.GetRace.Replace("{id:guid}", id.ToString()));

    private static string SegmentsRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", raceId.ToString()));

    private static string ItemsRoute(Guid raceId, Guid segmentId) =>
        Route(ApiEndpoints.Users.CreateItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{segmentId:guid}", segmentId.ToString()));

    private static string Route(string route) => $"/{route}";
}
