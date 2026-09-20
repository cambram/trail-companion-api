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
public class RaceEditingEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public RaceEditingEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EditingRoutes_WithoutAToken_Return401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var raceId = Guid.NewGuid();

        var race = await client.PutAsJsonAsync($"/api/races/{raceId}", new CreateRaceRequest("A", 1));
        var segments = await client.PutAsJsonAsync($"/api/races/{raceId}/segments/order", new ReorderRequest([]));
        var items = await client.PutAsJsonAsync(
            $"/api/races/{raceId}/segments/{Guid.NewGuid()}/items/order", new ReorderRequest([]));

        Assert.Equal(HttpStatusCode.Unauthorized, race.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, segments.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, items.StatusCode);
    }

    [Fact]
    public async Task UpdateRace_RenamesAndRetargets_AndRejectsForeignAndInvalid()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await CreateRace(owner);

        var ok = await owner.PutAsJsonAsync($"/api/races/{race.Id}", new CreateRaceRequest("UTCT 55K", 75));
        var invalid = await owner.PutAsJsonAsync($"/api/races/{race.Id}", new CreateRaceRequest("", 75));
        var foreign = await other.PutAsJsonAsync($"/api/races/{race.Id}", new CreateRaceRequest("Hacked", 1));

        var dto = await ok.Content.ReadFromJsonAsync<RaceDto>();
        Assert.Equal(("UTCT 55K", 75), (dto!.Name, dto.Target));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
    }

    [Fact]
    public async Task UpdateRace_StartTime_RoundTripsThroughDetail()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);

        await client.PutAsJsonAsync($"/api/races/{race.Id}", new CreateRaceRequest("UTMB", null, new TimeOnly(6, 30)));
        var detail = await client.GetFromJsonAsync<RaceDetailDto>($"/api/races/{race.Id}", TestJson.Options);

        Assert.Equal(new TimeOnly(6, 30), detail!.StartTime);
    }

    [Fact]
    public async Task UpdateRace_WithMalformedStartTime_Returns400()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);
        var race = await CreateRace(client);

        var response = await client.PutAsync($"/api/races/{race.Id}", new StringContent(
            """{"name":"UTMB","startTime":"25:00"}""", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reorder_PersistsSegmentAndItemOrder_AndRejectsIncompleteAndForeign()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignIn(owner);
        await SignIn(other);
        var race = await CreateRace(owner);
        var a = await CreateSegment(owner, race.Id, "A");
        var b = await CreateSegment(owner, race.Id, "B");
        var gel = await CreateItem(owner, race.Id, a.Id, "Gel");
        var banana = await CreateItem(owner, race.Id, a.Id, "Banana");
        var segmentsRoute = $"/api/races/{race.Id}/segments/order";
        var itemsRoute = $"/api/races/{race.Id}/segments/{a.Id}/items/order";

        var segments = await owner.PutAsJsonAsync(segmentsRoute, new ReorderRequest([b.Id, a.Id]));
        var items = await owner.PutAsJsonAsync(itemsRoute, new ReorderRequest([banana.Id, gel.Id]));
        var incomplete = await owner.PutAsJsonAsync(segmentsRoute, new ReorderRequest([a.Id]));
        var foreignSegments = await other.PutAsJsonAsync(segmentsRoute, new ReorderRequest([b.Id, a.Id]));

        Assert.Equal(HttpStatusCode.NoContent, segments.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, items.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignSegments.StatusCode);
        var detail = await owner.GetFromJsonAsync<RaceDetailDto>($"/api/races/{race.Id}", TestJson.Options);
        Assert.Equal(["B", "A"], detail!.Segments.Select(s => s.Name));
        Assert.Equal(["Banana", "Gel"], detail.Segments[1].Items.Select(i => i.Name));
    }

    private static async Task<RaceDto> CreateRace(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/races", new CreateRaceRequest("UTMB", null));
        return (await response.Content.ReadFromJsonAsync<RaceDto>())!;
    }

    private static async Task<SegmentDto> CreateSegment(HttpClient client, Guid raceId, string name)
    {
        var response = await client.PostAsJsonAsync($"/api/races/{raceId}/segments", new SegmentRequest(name, 30, null, null));
        return (await response.Content.ReadFromJsonAsync<SegmentDto>())!;
    }

    private static async Task<ItemDto> CreateItem(HttpClient client, Guid raceId, Guid segmentId, string name)
    {
        var nutrition = await client.PostAsJsonAsync(
            "/api/nutrition-items",
            new NutritionItemRequest(name, NutritionType.Gel, null, 25, null, null, null, null, null, null),
            TestJson.Options);
        var created = (await nutrition.Content.ReadFromJsonAsync<NutritionItemDto>(TestJson.Options))!;
        var response = await client.PostAsJsonAsync(
            $"/api/races/{raceId}/segments/{segmentId}/items", new ItemRequest(created.Id));
        return (await response.Content.ReadFromJsonAsync<ItemDto>(TestJson.Options))!;
    }

    private static async Task SignIn(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync($"/{ApiEndpoints.Users.RegisterUser}", new RegisterUserRequest(email, Password));
        var login = await client.PostAsJsonAsync($"/{ApiEndpoints.Users.LoginUser}", new LoginUserRequest(email, Password));
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));
}
