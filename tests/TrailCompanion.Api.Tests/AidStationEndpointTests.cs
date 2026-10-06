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
public class AidStationEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public AidStationEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AidStations_WithoutAToken_Return401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var raceId = Guid.NewGuid();
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await client.PostAsJsonAsync(StationsRoute(raceId), new CreateAidStationRequest(Guid.NewGuid(), "A")),
            await client.PutAsJsonAsync(StationRoute(raceId, id), new AidStationRequest("A", null, null)),
            await client.DeleteAsync(StationRoute(raceId, id)),
            await client.PutAsJsonAsync(MoveRoute(raceId, id), new MoveAidStationRequest(Guid.NewGuid())),
            await client.PostAsJsonAsync(ItemsRoute(raceId, id), new ItemRequest(Guid.NewGuid())),
            await client.PutAsJsonAsync(ItemsOrderRoute(raceId, id), new ReorderRequest([])),
            await client.DeleteAsync(ItemRoute(raceId, id, Guid.NewGuid()))
        };

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
    }

    [Fact]
    public async Task CreateUpdateAndRead_ShowsStationNestedUnderItsSegmentWithKitAndItems()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var segment = await CreateSegment(client, race.Id);
        var sandwich = await CreateNutrition(client, "Sandwich", 30);

        var created = await client.PostAsJsonAsync(StationsRoute(race.Id), new CreateAidStationRequest(segment.Id, "Hout Bay"));
        var station = await created.Content.ReadFromJsonAsync<AidStationDto>(TestJson.Options);
        var updated = await client.PutAsJsonAsync(
            StationRoute(race.Id, station!.Id),
            new AidStationRequest("Hout Bay", "10 min", [new KitEntryRequest("Socks", KitCertainty.Maybe)]),
            TestJson.Options);
        var item = await client.PostAsJsonAsync(ItemsRoute(race.Id, station.Id), new ItemRequest(sandwich.Id));
        var detail = await GetRace(client, race.Id);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.Created, item.StatusCode);
        var nested = detail.Segments.Single().AidStation;
        Assert.NotNull(nested);
        Assert.Equal("10 min", nested.Notes);
        Assert.Equal([new KitEntryDto("Socks", KitCertainty.Maybe)], nested.Kit);
        Assert.Equal([30], nested.Items.Select(i => i.Carbs));
    }

    [Fact]
    public async Task Create_OnSegmentThatHasAStation_Returns409()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var segment = await CreateSegment(client, race.Id);

        await client.PostAsJsonAsync(StationsRoute(race.Id), new CreateAidStationRequest(segment.Id, "A"));
        var second = await client.PostAsJsonAsync(StationsRoute(race.Id), new CreateAidStationRequest(segment.Id, "B"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Move_IntoOccupiedGap_SwapsStations()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var first = await CreateSegment(client, race.Id);
        var second = await CreateSegment(client, race.Id);
        var a = await CreateStation(client, race.Id, first.Id, "A");
        await CreateStation(client, race.Id, second.Id, "B");

        var moved = await client.PutAsJsonAsync(MoveRoute(race.Id, a.Id), new MoveAidStationRequest(second.Id));
        var detail = await GetRace(client, race.Id);

        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        Assert.Equal("B", detail.Segments.Single(s => s.Id == first.Id).AidStation!.Name);
        Assert.Equal("A", detail.Segments.Single(s => s.Id == second.Id).AidStation!.Name);
    }

    [Fact]
    public async Task Move_IntoEmptyGap_Reassigns()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var first = await CreateSegment(client, race.Id);
        var second = await CreateSegment(client, race.Id);
        var a = await CreateStation(client, race.Id, first.Id, "A");

        await client.PutAsJsonAsync(MoveRoute(race.Id, a.Id), new MoveAidStationRequest(second.Id));
        var detail = await GetRace(client, race.Id);

        Assert.Null(detail.Segments.Single(s => s.Id == first.Id).AidStation);
        Assert.Equal("A", detail.Segments.Single(s => s.Id == second.Id).AidStation!.Name);
    }

    [Fact]
    public async Task AnotherUsersStation_Returns404Everywhere()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await CreateRace(owner);
        var segment = await CreateSegment(owner, race.Id);
        var station = await CreateStation(owner, race.Id, segment.Id, "Mine");
        var otherNutrition = await CreateNutrition(other, "Theirs", 10);

        var responses = new[]
        {
            await other.PostAsJsonAsync(StationsRoute(race.Id), new CreateAidStationRequest(segment.Id, "X")),
            await other.PutAsJsonAsync(StationRoute(race.Id, station.Id), new AidStationRequest("X", null, null)),
            await other.PutAsJsonAsync(MoveRoute(race.Id, station.Id), new MoveAidStationRequest(segment.Id)),
            await other.PostAsJsonAsync(ItemsRoute(race.Id, station.Id), new ItemRequest(otherNutrition.Id)),
            await other.PutAsJsonAsync(ItemsOrderRoute(race.Id, station.Id), new ReorderRequest([Guid.NewGuid()])),
            await other.DeleteAsync(ItemRoute(race.Id, station.Id, Guid.NewGuid())),
            await other.DeleteAsync(StationRoute(race.Id, station.Id))
        };

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
    }

    [Fact]
    public async Task DeletingALibraryItemUsedOnlyAtAStation_Returns409()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var segment = await CreateSegment(client, race.Id);
        var station = await CreateStation(client, race.Id, segment.Id, "A");
        var sandwich = await CreateNutrition(client, "Sandwich", 30);
        await client.PostAsJsonAsync(ItemsRoute(race.Id, station.Id), new ItemRequest(sandwich.Id));

        var delete = await client.DeleteAsync(Route(ApiEndpoints.Users.DeleteNutritionItem.Replace("{id:guid}", sandwich.Id.ToString())));

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task StationItems_ReorderAndDelete()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var segment = await CreateSegment(client, race.Id);
        var station = await CreateStation(client, race.Id, segment.Id, "A");
        var first = await (await client.PostAsJsonAsync(ItemsRoute(race.Id, station.Id), new ItemRequest((await CreateNutrition(client, "One", 10)).Id)))
            .Content.ReadFromJsonAsync<AidStationItemDto>(TestJson.Options);
        var second = await (await client.PostAsJsonAsync(ItemsRoute(race.Id, station.Id), new ItemRequest((await CreateNutrition(client, "Two", 20)).Id)))
            .Content.ReadFromJsonAsync<AidStationItemDto>(TestJson.Options);

        var reorder = await client.PutAsJsonAsync(ItemsOrderRoute(race.Id, station.Id), new ReorderRequest([second!.Id, first!.Id]));
        var afterReorder = await GetRace(client, race.Id);
        var delete = await client.DeleteAsync(ItemRoute(race.Id, station.Id, first.Id));
        var afterDelete = await GetRace(client, race.Id);

        Assert.Equal(HttpStatusCode.NoContent, reorder.StatusCode);
        Assert.Equal(["Two", "One"], afterReorder.Segments.Single().AidStation!.Items.OrderBy(i => i.Position).Select(i => i.Name));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(["Two"], afterDelete.Segments.Single().AidStation!.Items.Select(i => i.Name));
    }

    private static async Task<RaceDto> CreateRace(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTCT", null));
        return (await response.Content.ReadFromJsonAsync<RaceDto>())!;
    }

    private static async Task<SegmentDto> CreateSegment(HttpClient client, Guid raceId)
    {
        var path = Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", raceId.ToString()));
        var response = await client.PostAsJsonAsync(path, new SegmentRequest("Seg", 30, null, null));
        return (await response.Content.ReadFromJsonAsync<SegmentDto>())!;
    }

    private static async Task<AidStationDto> CreateStation(HttpClient client, Guid raceId, Guid segmentId, string name)
    {
        var response = await client.PostAsJsonAsync(StationsRoute(raceId), new CreateAidStationRequest(segmentId, name));
        return (await response.Content.ReadFromJsonAsync<AidStationDto>(TestJson.Options))!;
    }

    private static async Task<NutritionItemDto> CreateNutrition(HttpClient client, string name, int carbs)
    {
        var response = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.CreateNutritionItem),
            new NutritionItemRequest(name, NutritionType.Solid, null, carbs, null, null, null, null, null, null),
            TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<NutritionItemDto>(TestJson.Options))!;
    }

    private static async Task<RaceDetailDto> GetRace(HttpClient client, Guid raceId)
    {
        var response = await client.GetAsync(Route(ApiEndpoints.Users.GetRace.Replace("{id:guid}", raceId.ToString())));
        return (await response.Content.ReadFromJsonAsync<RaceDetailDto>(TestJson.Options))!;
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

    private static string StationsRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.CreateAidStation.Replace("{raceId:guid}", raceId.ToString()));

    private static string StationRoute(Guid raceId, Guid id) =>
        Route(ApiEndpoints.Users.UpdateAidStation
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{id:guid}", id.ToString()));

    private static string MoveRoute(Guid raceId, Guid id) =>
        Route(ApiEndpoints.Users.MoveAidStation
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{id:guid}", id.ToString()));

    private static string ItemsRoute(Guid raceId, Guid aidStationId) =>
        Route(ApiEndpoints.Users.CreateAidStationItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{aidStationId:guid}", aidStationId.ToString()));

    private static string ItemsOrderRoute(Guid raceId, Guid aidStationId) =>
        Route(ApiEndpoints.Users.ReorderAidStationItems
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{aidStationId:guid}", aidStationId.ToString()));

    private static string ItemRoute(Guid raceId, Guid aidStationId, Guid id) =>
        Route(ApiEndpoints.Users.DeleteAidStationItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{aidStationId:guid}", aidStationId.ToString())
            .Replace("{id:guid}", id.ToString()));

    private static string Route(string route) => $"/{route}";
}
