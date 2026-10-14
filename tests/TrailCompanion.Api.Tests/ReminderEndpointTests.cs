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
public class ReminderEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public ReminderEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReminderRoutes_WithoutAToken_Return401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var raceId = Guid.NewGuid();

        var responses = new[]
        {
            await client.PostAsJsonAsync(ItemsRoute(raceId, 1), new ItemRequest(Guid.NewGuid())),
            await client.PutAsJsonAsync(OrderRoute(raceId, 1), new ReorderRequest([])),
            await client.DeleteAsync(ItemRoute(raceId, 1, Guid.NewGuid()))
        };

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
    }

    [Fact]
    public async Task CreateReorderAndDelete_ShowUpGroupedByNumberInTheRaceRead()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var stroopwafel = await CreateNutrition(client, "Stroopwafel", NutritionType.Solid, 31);
        var gel = await CreateNutrition(client, "Gel", NutritionType.Gel, 40);

        var first = await client.PostAsJsonAsync(ItemsRoute(race.Id, 2), new ItemRequest(stroopwafel.Id));
        var second = await client.PostAsJsonAsync(ItemsRoute(race.Id, 2), new ItemRequest(gel.Id));
        var other = await client.PostAsJsonAsync(ItemsRoute(race.Id, 5), new ItemRequest(gel.Id));
        var firstItem = await first.Content.ReadFromJsonAsync<ReminderItemDto>(TestJson.Options);
        var secondItem = await second.Content.ReadFromJsonAsync<ReminderItemDto>(TestJson.Options);
        var detail = await GetRace(client, race.Id);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
        Assert.Equal(30, detail.ReminderIntervalMin);
        Assert.Equal([2, 5], detail.Reminders.Select(r => r.Number));
        Assert.Equal(["Stroopwafel", "Gel"], detail.Reminders[0].Items.OrderBy(i => i.Position).Select(i => i.Name));

        var reorder = await client.PutAsJsonAsync(OrderRoute(race.Id, 2), new ReorderRequest([secondItem!.Id, firstItem!.Id]));
        var afterReorder = await GetRace(client, race.Id);
        var delete = await client.DeleteAsync(ItemRoute(race.Id, 2, secondItem.Id));
        var afterDelete = await GetRace(client, race.Id);

        Assert.Equal(HttpStatusCode.NoContent, reorder.StatusCode);
        Assert.Equal(["Gel", "Stroopwafel"], afterReorder.Reminders[0].Items.OrderBy(i => i.Position).Select(i => i.Name));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(["Stroopwafel"], afterDelete.Reminders[0].Items.Select(i => i.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Create_WithReminderNumberOutOfRange_Returns400(int number)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var gel = await CreateNutrition(client, "Gel", NutritionType.Gel, 40);

        var response = await client.PostAsJsonAsync(ItemsRoute(race.Id, number), new ItemRequest(gel.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AboveTheTenItemLimit_Returns400()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var gel = await CreateNutrition(client, "Gel", NutritionType.Gel, 40);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(ItemsRoute(race.Id, 1), new ItemRequest(gel.Id))).StatusCode);
        }
        var eleventh = await client.PostAsJsonAsync(ItemsRoute(race.Id, 1), new ItemRequest(gel.Id));

        Assert.Equal(HttpStatusCode.BadRequest, eleventh.StatusCode);
    }

    [Fact]
    public async Task AnotherUsersRaceOrLibraryItem_Returns404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var stranger = factory.CreateClient();
        await SignIn(owner);
        await SignIn(stranger);
        var race = await CreateRace(owner);
        var ownersGel = await CreateNutrition(owner, "Gel", NutritionType.Gel, 40);
        var strangersGel = await CreateNutrition(stranger, "Gel", NutritionType.Gel, 40);
        var item = await (await owner.PostAsJsonAsync(ItemsRoute(race.Id, 1), new ItemRequest(ownersGel.Id)))
            .Content.ReadFromJsonAsync<ReminderItemDto>(TestJson.Options);

        var createOnForeignRace = await stranger.PostAsJsonAsync(ItemsRoute(race.Id, 1), new ItemRequest(strangersGel.Id));
        var createWithForeignItem = await owner.PostAsJsonAsync(ItemsRoute(race.Id, 1), new ItemRequest(strangersGel.Id));
        var reorderForeign = await stranger.PutAsJsonAsync(OrderRoute(race.Id, 1), new ReorderRequest([item!.Id]));
        var deleteForeign = await stranger.DeleteAsync(ItemRoute(race.Id, 1, item.Id));

        Assert.Equal(HttpStatusCode.NotFound, createOnForeignRace.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, createWithForeignItem.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reorderForeign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteForeign.StatusCode);
    }

    [Fact]
    public async Task SegmentItems_AcceptOnlyLiquidLibraryItems()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var segment = await CreateSegment(client, race.Id);
        var gel = await CreateNutrition(client, "Gel", NutritionType.Gel, 40);
        var mix = await CreateNutrition(client, "Carb mix", NutritionType.Liquid, 20);

        var rejected = await client.PostAsJsonAsync(SegmentItemsRoute(race.Id, segment.Id), new ItemRequest(gel.Id));
        var accepted = await client.PostAsJsonAsync(SegmentItemsRoute(race.Id, segment.Id), new ItemRequest(mix.Id));

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    }

    [Fact]
    public async Task DeletingALibraryItemUsedByAReminder_Returns409()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);
        var gel = await CreateNutrition(client, "Gel", NutritionType.Gel, 40);
        await client.PostAsJsonAsync(ItemsRoute(race.Id, 1), new ItemRequest(gel.Id));

        var response = await client.DeleteAsync(
            Route(ApiEndpoints.Users.DeleteNutritionItem.Replace("{id:guid}", gel.Id.ToString())));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<RaceDto> CreateRace(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTCT", null));
        return (await response.Content.ReadFromJsonAsync<RaceDto>(TestJson.Options))!;
    }

    private static async Task<SegmentDto> CreateSegment(HttpClient client, Guid raceId)
    {
        var path = Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", raceId.ToString()));
        var response = await client.PostAsJsonAsync(path, new SegmentRequest("Seg", 30, null, null));
        return (await response.Content.ReadFromJsonAsync<SegmentDto>())!;
    }

    private static async Task<NutritionItemDto> CreateNutrition(HttpClient client, string name, NutritionType type, int carbs)
    {
        var response = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.CreateNutritionItem),
            new NutritionItemRequest(name, type, null, carbs, null, null, null, null, null, null),
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

    private static string ItemsRoute(Guid raceId, int number) =>
        Route(ApiEndpoints.Users.CreateReminderItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{number:int}", number.ToString()));

    private static string OrderRoute(Guid raceId, int number) =>
        Route(ApiEndpoints.Users.ReorderReminderItems
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{number:int}", number.ToString()));

    private static string ItemRoute(Guid raceId, int number, Guid id) =>
        Route(ApiEndpoints.Users.DeleteReminderItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{number:int}", number.ToString())
            .Replace("{id:guid}", id.ToString()));

    private static string SegmentItemsRoute(Guid raceId, Guid segmentId) =>
        Route(ApiEndpoints.Users.CreateItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{segmentId:guid}", segmentId.ToString()));

    private static string Route(string route) => $"/{route}";
}
