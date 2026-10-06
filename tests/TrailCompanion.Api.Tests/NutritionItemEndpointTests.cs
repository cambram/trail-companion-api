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
public class NutritionItemEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public NutritionItemEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NutritionItems_WithoutAToken_Return401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        var body = Request("A");

        var list = await client.GetAsync(Route(ApiEndpoints.Users.GetNutritionItems));
        var create = await client.PostAsJsonAsync(
Route(ApiEndpoints.Users.CreateNutritionItem), body, TestJson.Options);
        var update = await client.PutAsJsonAsync(ItemRoute(id), body, TestJson.Options);
        var delete = await client.DeleteAsync(ItemRoute(id));

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task CreateAndList_ReturnsCallersLibraryOrderedByName()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);

        var created = await Create(client, Request("Zip gel", NutritionType.Gel, null, 25));
        await Create(client, Request("Alpha flask", NutritionType.Liquid, 500, 50));

        Assert.Equal("Zip gel", created.Name);
        Assert.Equal(25, created.Carbs);
        Assert.Null(created.Sodium);
        var list = (await client.GetFromJsonAsync<List<NutritionItemDto>>(Route(ApiEndpoints.Users.GetNutritionItems), TestJson.Options))!;
        Assert.Equal(["Alpha flask", "Zip gel"], list.Select(n => n.Name));
        Assert.Equal(NutritionType.Liquid, list[0].Type);
        Assert.Equal(500, list[0].Size);
    }

    [Fact]
    public async Task Create_KeepsDecimalMineralsAndRoundsNothingOff()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var request = new NutritionItemRequest(
            "Precision PH 1500", NutritionType.Liquid, 500, 0, 0, null, 1512.5m, 45.25m, 0.5m, 12.75m);

        var created = await Create(client, request);

        Assert.Equal(1512.5m, created.Sodium);
        Assert.Equal(45.25m, created.Potassium);
        Assert.Equal(0.5m, created.Magnesium);
        Assert.Equal(12.75m, created.Calcium);
        var list = (await client.GetFromJsonAsync<List<NutritionItemDto>>(
            Route(ApiEndpoints.Users.GetNutritionItems), TestJson.Options))!;
        var reloaded = Assert.Single(list);
        Assert.Equal(1512.5m, reloaded.Sodium);
        Assert.Equal(0.5m, reloaded.Magnesium);
        Assert.Equal(0, reloaded.Carbs);
    }

    [Fact]
    public async Task List_DoesNotLeakAnotherUsersLibrary()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        await Create(owner, Request("Mine"));

        var list = await other.GetFromJsonAsync<List<NutritionItemDto>>(Route(ApiEndpoints.Users.GetNutritionItems), TestJson.Options);

        Assert.Empty(list!);
    }

    [Fact]
    public async Task Create_WithADuplicateName_Returns409()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        await Create(client, Request("Maurten 100"));

        var response = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.CreateNutritionItem), Request("Maurten 100"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAnInvalidBody_Returns400()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);

        var blankName = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.CreateNutritionItem), Request("  "), TestJson.Options);
        var negative = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.CreateNutritionItem), Request("A", NutritionType.Gel, null, -1), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesEveryFieldAndBlanksOmittedMacros()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var created = await Create(client, Request("Old", NutritionType.Gel, 40, 25));

        var response = await client.PutAsJsonAsync(
            ItemRoute(created.Id), Request("New", NutritionType.Liquid, 500, null), TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<NutritionItemDto>(TestJson.Options);
        Assert.Equal("New", updated!.Name);
        Assert.Equal(NutritionType.Liquid, updated.Type);
        Assert.Equal(500, updated.Size);
        Assert.Null(updated.Carbs);
    }

    [Fact]
    public async Task UpdateOrDelete_AnotherUsersItem_Returns404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var created = await Create(owner, Request("Mine"));

        var update = await other.PutAsJsonAsync(ItemRoute(created.Id), Request("Hacked"), TestJson.Options);
        var delete = await other.DeleteAsync(ItemRoute(created.Id));

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenUnused_Returns204()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var created = await Create(client, Request("Spare"));

        var first = await client.DeleteAsync(ItemRoute(created.Id));
        var second = await client.DeleteAsync(ItemRoute(created.Id));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenUsedByASegment_Returns409AndKeepsIt()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var created = await Create(client, Request("In use"));
        var race = await Post<RaceDto>(client, Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", null));
        var segment = await Post<SegmentDto>(
            client,
            Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", race.Id.ToString())),
            new SegmentRequest("Seg", 30, null, null));
        await Post<ItemDto>(
            client,
            Route(ApiEndpoints.Users.CreateItem
                .Replace("{raceId:guid}", race.Id.ToString())
                .Replace("{segmentId:guid}", segment.Id.ToString())),
            new ItemRequest(created.Id));

        var response = await client.DeleteAsync(ItemRoute(created.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var list = await client.GetFromJsonAsync<List<NutritionItemDto>>(Route(ApiEndpoints.Users.GetNutritionItems), TestJson.Options);
        Assert.Single(list!);
    }

    private static NutritionItemRequest Request(
        string name,
        NutritionType type = NutritionType.Gel,
        int? size = null,
        int? carbs = 40) =>
        new(name, type, size, carbs, null, null, null, null, null, null);

    private static Task<NutritionItemDto> Create(HttpClient client, NutritionItemRequest request) =>
        Post<NutritionItemDto>(client, Route(ApiEndpoints.Users.CreateNutritionItem), request);

    private static async Task<T> Post<T>(HttpClient client, string route, object body)
    {
        var response = await client.PostAsJsonAsync(route, body, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(TestJson.Options))!;
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

    private static string ItemRoute(Guid id) =>
        Route(ApiEndpoints.Users.UpdateNutritionItem.Replace("{id:guid}", id.ToString()));

    private static string Route(string route) => $"/{route}";
}
