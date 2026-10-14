using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class SegmentEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public SegmentEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Segments_WithoutAToken_Return401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var raceId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();
        var body = new SegmentRequest("A", 1, null, null);

        var create = await client.PostAsJsonAsync(CreateRoute(raceId), body);
        var update = await client.PutAsJsonAsync(ItemRoute(raceId, segmentId), body);
        var delete = await client.DeleteAsync(ItemRoute(raceId, segmentId));

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task CreateSegment_Returns201AndAppendsPositions()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);

        var first = await client.PostAsJsonAsync(CreateRoute(race.Id), new SegmentRequest("Start to Aid 1", 90, 11, "Easy"));
        var second = await client.PostAsJsonAsync(CreateRoute(race.Id), new SegmentRequest("Aid 1 to Aid 2", 60, null, null));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstDto = await first.Content.ReadFromJsonAsync<SegmentDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<SegmentDto>();
        Assert.Equal(1, firstDto!.Position);
        Assert.Equal(2, secondDto!.Position);
        Assert.Equal(race.Id, firstDto.RaceId);
        Assert.Equal("Easy", firstDto.Notes);
    }

    [Fact]
    public async Task CreateSegment_WithInvalidBody_Returns400()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);

        var response = await client.PostAsJsonAsync(CreateRoute(race.Id), new SegmentRequest("", 10, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSegment_OnAnotherUsersRace_Returns404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await CreateRace(owner);

        var response = await other.PostAsJsonAsync(CreateRoute(race.Id), new SegmentRequest("A", 1, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSegment_SendsAndReturnsEndKmOnTheWire()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);

        var response = await client.PostAsJsonAsync(CreateRoute(race.Id), new { name = "Start to Aid 1", duration = 90, endKm = 20.1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(20.1, json.RootElement.GetProperty("endKm").GetDouble());
        Assert.False(json.RootElement.TryGetProperty("distance", out _));
    }

    [Fact]
    public async Task CreateSegment_SendsAndReturnsAscentOnTheWire_AndRejectsGarbage()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);

        var ok = await client.PostAsJsonAsync(CreateRoute(race.Id), new { name = "Climb", duration = 90, ascentM = 650 });
        var negative = await client.PostAsJsonAsync(CreateRoute(race.Id), new { name = "Bad", duration = 90, ascentM = -5 });
        var fractional = await client.PostAsJsonAsync(CreateRoute(race.Id), new { name = "Bad", duration = 90, ascentM = 12.5 });

        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        using var json = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());
        Assert.Equal(650, json.RootElement.GetProperty("ascentM").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, fractional.StatusCode);
    }

    [Fact]
    public async Task UpdateSegment_ReplacesFieldsAndKeepsPosition()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var segment = await CreateSegment(client, race.Id, "Old");

        var response = await client.PutAsJsonAsync(ItemRoute(race.Id, segment.Id), new SegmentRequest("New", 45, 7.5, "Notes"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SegmentDto>();
        Assert.Equal("New", updated!.Name);
        Assert.Equal(45, updated.Duration);
        Assert.Equal(7.5, updated.EndKm);
        Assert.Equal("Notes", updated.Notes);
        Assert.Equal(segment.Position, updated.Position);
    }

    [Fact]
    public async Task UpdateSegment_AnotherUsersSegmentOrWrongRace_Returns404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await CreateRace(owner);
        var otherRace = await CreateRace(owner);
        var segment = await CreateSegment(owner, race.Id, "Mine");
        var body = new SegmentRequest("Hacked", 1, null, null);

        var foreign = await other.PutAsJsonAsync(ItemRoute(race.Id, segment.Id), body);
        var wrongRace = await owner.PutAsJsonAsync(ItemRoute(otherRace.Id, segment.Id), body);

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, wrongRace.StatusCode);
    }

    [Fact]
    public async Task DeleteSegment_Returns204AndSecondDeleteReturns404()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var segment = await CreateSegment(client, race.Id, "A");

        var first = await client.DeleteAsync(ItemRoute(race.Id, segment.Id));
        var second = await client.DeleteAsync(ItemRoute(race.Id, segment.Id));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task DeleteSegment_AnotherUsersSegment_Returns404AndKeepsIt()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await CreateRace(owner);
        var segment = await CreateSegment(owner, race.Id, "Mine");

        var foreign = await other.DeleteAsync(ItemRoute(race.Id, segment.Id));
        var mine = await owner.DeleteAsync(ItemRoute(race.Id, segment.Id));

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, mine.StatusCode);
    }

    private static async Task<RaceDto> CreateRace(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", null));
        return (await response.Content.ReadFromJsonAsync<RaceDto>(TestJson.Options))!;
    }

    private static async Task<SegmentDto> CreateSegment(HttpClient client, Guid raceId, string name)
    {
        var response = await client.PostAsJsonAsync(CreateRoute(raceId), new SegmentRequest(name, 30, null, null));
        return (await response.Content.ReadFromJsonAsync<SegmentDto>())!;
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

    private static string CreateRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", raceId.ToString()));

    private static string ItemRoute(Guid raceId, Guid id) =>
        Route(ApiEndpoints.Users.UpdateSegment
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{id:guid}", id.ToString()));

    private static string Route(string route) => $"/{route}";
}
