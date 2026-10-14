using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class ItemEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public ItemEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Items_WithoutAToken_Return401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var raceId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var create = await client.PostAsJsonAsync(CreateRoute(raceId, segmentId), new ItemRequest(Guid.NewGuid()));
        var delete = await client.DeleteAsync(ItemRoute(raceId, segmentId, itemId));

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task CreateItem_Returns201WithLibraryValuesAndAppendsPositions()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var (race, segment) = await CreateRaceWithSegment(client);
        var gel = await CreateNutrition(client, "Maurten Gel 160");
        var flask = await CreateNutrition(client, "Tailwind flask", NutritionType.Liquid, 500, 50);

        var first = await client.PostAsJsonAsync(CreateRoute(race.Id, segment.Id), new ItemRequest(gel.Id));
        var second = await client.PostAsJsonAsync(CreateRoute(race.Id, segment.Id), new ItemRequest(flask.Id));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstDto = await first.Content.ReadFromJsonAsync<ItemDto>(TestJson.Options);
        var secondDto = await second.Content.ReadFromJsonAsync<ItemDto>(TestJson.Options);
        Assert.Equal(1, firstDto!.Position);
        Assert.Equal("Maurten Gel 160", firstDto.Name);
        Assert.Equal(40, firstDto.Carbs);
        Assert.Equal(gel.Id, firstDto.NutritionItemId);
        Assert.Equal(segment.Id, firstDto.SegmentId);
        Assert.Equal(2, secondDto!.Position);
        Assert.Equal(NutritionType.Liquid, secondDto.Type);
        Assert.Equal(500, secondDto.Size);
    }

    [Fact]
    public async Task CreateItem_TwiceWithTheSameLibraryItem_CreatesTwoRows()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var (race, segment) = await CreateRaceWithSegment(client);
        var gel = await CreateNutrition(client, "Maurten 100");

        var first = await client.PostAsJsonAsync(CreateRoute(race.Id, segment.Id), new ItemRequest(gel.Id));
        var second = await client.PostAsJsonAsync(CreateRoute(race.Id, segment.Id), new ItemRequest(gel.Id));

        var firstDto = await first.Content.ReadFromJsonAsync<ItemDto>(TestJson.Options);
        var secondDto = await second.Content.ReadFromJsonAsync<ItemDto>(TestJson.Options);
        Assert.NotEqual(firstDto!.Id, secondDto!.Id);
        Assert.Equal(2, secondDto.Position);
    }

    [Fact]
    public async Task CreateItem_WithoutANutritionItemId_Returns400()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var (race, segment) = await CreateRaceWithSegment(client);

        var response = await client.PostAsJsonAsync(CreateRoute(race.Id, segment.Id), new ItemRequest(null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_WithAnotherUsersNutritionItem_Returns404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var (race, segment) = await CreateRaceWithSegment(owner);
        var foreignNutrition = await CreateNutrition(other, "Theirs");

        var response = await owner.PostAsJsonAsync(CreateRoute(race.Id, segment.Id), new ItemRequest(foreignNutrition.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_OnAnotherUsersSegmentOrWrongRace_Returns404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var (race, segment) = await CreateRaceWithSegment(owner);
        var otherRace = await CreateRace(owner);
        var nutrition = await CreateNutrition(owner, "Mine");

        var foreign = await other.PostAsJsonAsync(CreateRoute(race.Id, segment.Id), new ItemRequest(nutrition.Id));
        var wrongRace = await owner.PostAsJsonAsync(CreateRoute(otherRace.Id, segment.Id), new ItemRequest(nutrition.Id));

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, wrongRace.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_Returns204AndSecondDeleteReturns404()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var (race, segment) = await CreateRaceWithSegment(client);
        var item = await CreateItem(client, race.Id, segment.Id, "A");

        var first = await client.DeleteAsync(ItemRoute(race.Id, segment.Id, item.Id));
        var second = await client.DeleteAsync(ItemRoute(race.Id, segment.Id, item.Id));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_AnotherUsersItem_Returns404AndKeepsIt()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var (race, segment) = await CreateRaceWithSegment(owner);
        var item = await CreateItem(owner, race.Id, segment.Id, "Mine");

        var foreign = await other.DeleteAsync(ItemRoute(race.Id, segment.Id, item.Id));
        var mine = await owner.DeleteAsync(ItemRoute(race.Id, segment.Id, item.Id));

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, mine.StatusCode);
    }

    [Fact]
    public async Task DeleteSegment_CascadesItsItems()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var (race, segment) = await CreateRaceWithSegment(client);
        var item = await CreateItem(client, race.Id, segment.Id, "A");
        var deleteSegmentRoute = Route(ApiEndpoints.Users.DeleteSegment
            .Replace("{raceId:guid}", race.Id.ToString())
            .Replace("{id:guid}", segment.Id.ToString()));

        var deleted = await client.DeleteAsync(deleteSegmentRoute);
        var itemAfter = await client.DeleteAsync(ItemRoute(race.Id, segment.Id, item.Id));

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, itemAfter.StatusCode);
    }

    private static async Task<RaceDto> CreateRace(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", null));
        return (await response.Content.ReadFromJsonAsync<RaceDto>(TestJson.Options))!;
    }

    private static async Task<SegmentDto> CreateSegment(HttpClient client, Guid raceId)
    {
        var path = Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", raceId.ToString()));
        var response = await client.PostAsJsonAsync(path, new SegmentRequest("Seg", 30, null, null));
        return (await response.Content.ReadFromJsonAsync<SegmentDto>())!;
    }

    private static async Task<(RaceDto Race, SegmentDto Segment)> CreateRaceWithSegment(HttpClient client)
    {
        var race = await CreateRace(client);
        return (race, await CreateSegment(client, race.Id));
    }

    private static async Task<NutritionItemDto> CreateNutrition(
        HttpClient client,
        string name,
        NutritionType type = NutritionType.Liquid,
        int? size = null,
        int? carbs = 40)
    {
        var response = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.CreateNutritionItem),
            new NutritionItemRequest(name, type, size, carbs, null, null, null, null, null, null),
            TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<NutritionItemDto>(TestJson.Options))!;
    }

    private static async Task<ItemDto> CreateItem(HttpClient client, Guid raceId, Guid segmentId, string name)
    {
        var nutrition = await CreateNutrition(client, name);
        var response = await client.PostAsJsonAsync(CreateRoute(raceId, segmentId), new ItemRequest(nutrition.Id));
        return (await response.Content.ReadFromJsonAsync<ItemDto>(TestJson.Options))!;
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

    private static string CreateRoute(Guid raceId, Guid segmentId) =>
        Route(ApiEndpoints.Users.CreateItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{segmentId:guid}", segmentId.ToString()));

    private static string ItemRoute(Guid raceId, Guid segmentId, Guid id) =>
        Route(ApiEndpoints.Users.DeleteItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{segmentId:guid}", segmentId.ToString())
            .Replace("{id:guid}", id.ToString()));

    private static string Route(string route) => $"/{route}";
}
