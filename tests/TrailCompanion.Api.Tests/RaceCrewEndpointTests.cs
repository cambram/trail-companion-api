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
public class RaceCrewEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public RaceCrewEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Crew_JoinByPin_CanReadThePlanWithOwnersItems_ButEveryWriteIs404()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var crew = factory.CreateClient();
        await SignIn(owner);
        await SignIn(crew);
        var race = await Post<RaceDto>(owner, Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTCT", 70));
        var segment = await Post<SegmentDto>(owner, SegmentsRoute(race.Id), new SegmentRequest("Start to AS1", 180, 18, null));
        var gel = await Post<NutritionItemDto>(owner, Route(ApiEndpoints.Users.CreateNutritionItem),
            new NutritionItemRequest("Gel", NutritionType.Liquid, null, 40, null, null, null, null, null, null));
        var item = await Post<ItemDto>(owner, ItemsRoute(race.Id, segment.Id), new ItemRequest(gel.Id));
        var pin = (await owner.GetFromJsonAsync<RaceDetailDto>(RaceRoute(race.Id), TestJson.Options))!.Pin!;

        var joined = await Post<RaceDto>(crew, Route(ApiEndpoints.Users.JoinRace), new JoinRaceRequest(pin));
        var detail = await crew.GetFromJsonAsync<RaceDetailDto>(RaceRoute(race.Id), TestJson.Options);
        var list = await crew.GetFromJsonAsync<List<RaceDto>>(Route(ApiEndpoints.Users.GetRaces), TestJson.Options);

        Assert.Equal(RaceRole.Crew, joined.Role);
        Assert.Equal(RaceRole.Crew, detail!.Role);
        Assert.Null(detail.Pin);
        Assert.Equal(["Gel"], Assert.Single(detail.Segments).Items.Select(i => i.Name));
        Assert.Equal(RaceRole.Crew, Assert.Single(list!).Role);

        Assert.Equal(HttpStatusCode.NotFound,
            (await crew.PutAsJsonAsync(RaceRoute(race.Id), new CreateRaceRequest("Hijacked", 1), TestJson.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await crew.PostAsJsonAsync(SegmentsRoute(race.Id), new SegmentRequest("X", 1, null, null), TestJson.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await crew.DeleteAsync(RaceRoute(race.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await crew.PostAsync(PinRoute(race.Id), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await crew.PostAsJsonAsync(AidStationsRoute(race.Id), new CreateAidStationRequest(segment.Id, "AS1"), TestJson.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await crew.DeleteAsync(Route(ApiEndpoints.Users.DeleteItem
                .Replace("{raceId:guid}", race.Id.ToString())
                .Replace("{segmentId:guid}", segment.Id.ToString())
                .Replace("{id:guid}", item.Id.ToString())))).StatusCode);

        var members = await crew.GetFromJsonAsync<List<CrewMemberDto>>(CrewRoute(race.Id), TestJson.Options);
        Assert.Single(members!);
        var after = await owner.GetFromJsonAsync<RaceDetailDto>(RaceRoute(race.Id), TestJson.Options);
        Assert.Equal("UTCT", after!.Name);
        Assert.Equal(["Gel"], Assert.Single(after.Segments).Items.Select(i => i.Name));
        Assert.Null(Assert.Single(after.Segments).AidStation);
    }

    [Fact]
    public async Task Join_Twice_IsIdempotent_AndOwnUnknownOrMalformedPinsFail()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var crew = factory.CreateClient();
        await SignIn(owner);
        await SignIn(crew);
        var (raceId, pin) = await CreateRace(owner);

        await Post<RaceDto>(crew, Route(ApiEndpoints.Users.JoinRace), new JoinRaceRequest(pin));
        await Post<RaceDto>(crew, Route(ApiEndpoints.Users.JoinRace), new JoinRaceRequest(pin));

        Assert.Single((await owner.GetFromJsonAsync<List<CrewMemberDto>>(CrewRoute(raceId), TestJson.Options))!);
        Assert.Equal(HttpStatusCode.BadRequest, (await Join(owner, pin)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Join(crew, "12ab56")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Join(crew, pin == "999999" ? "999998" : "999999")).StatusCode);
    }

    [Fact]
    public async Task RemovedCrew_LoseAccess_CrewCanLeave_ButCannotRemoveOthers()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var first = factory.CreateClient();
        using var second = factory.CreateClient();
        await SignIn(owner);
        var firstId = await SignIn(first);
        var secondId = await SignIn(second);
        var (raceId, pin) = await CreateRace(owner);
        await Join(first, pin);
        await Join(second, pin);

        Assert.Equal(HttpStatusCode.NotFound, (await first.DeleteAsync(MemberRoute(raceId, secondId))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(MemberRoute(raceId, secondId))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.DeleteAsync(MemberRoute(raceId, firstId))).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await first.GetAsync(RaceRoute(raceId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await second.GetAsync(RaceRoute(raceId))).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<List<CrewMemberDto>>(CrewRoute(raceId), TestJson.Options))!);
    }

    [Fact]
    public async Task Stranger_CannotSeeTheRaceOrItsCrew()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var stranger = factory.CreateClient();
        await SignIn(owner);
        await SignIn(stranger);
        var (raceId, _) = await CreateRace(owner);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(RaceRoute(raceId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(CrewRoute(raceId))).StatusCode);
    }

    [Fact]
    public async Task RegeneratePin_OldPinStopsWorking()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var crew = factory.CreateClient();
        await SignIn(owner);
        await SignIn(crew);
        var (raceId, oldPin) = await CreateRace(owner);

        var fresh = await Post<RacePinDto>(owner, PinRoute(raceId), new { });

        Assert.NotEqual(oldPin, fresh.Pin);
        Assert.Equal(HttpStatusCode.NotFound, (await Join(crew, oldPin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Join(crew, fresh.Pin)).StatusCode);
    }

    [Fact]
    public async Task DeletingAccounts_OnEitherSide_CleansUpMembership()
    {
        using var factory = CreateFactory();
        using var owner = factory.CreateClient();
        using var leaving = factory.CreateClient();
        using var staying = factory.CreateClient();
        await SignIn(owner);
        await SignIn(leaving);
        await SignIn(staying);
        var (raceId, pin) = await CreateRace(owner);
        Assert.Equal(HttpStatusCode.OK, (await Join(leaving, pin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Join(staying, pin)).StatusCode);
        Assert.Equal(2, (await owner.GetFromJsonAsync<List<CrewMemberDto>>(CrewRoute(raceId), TestJson.Options))!.Count);

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAccount(leaving)).StatusCode);
        Assert.Single((await owner.GetFromJsonAsync<List<CrewMemberDto>>(CrewRoute(raceId), TestJson.Options))!);
        Assert.Single((await staying.GetFromJsonAsync<List<RaceDto>>(Route(ApiEndpoints.Users.GetRaces), TestJson.Options))!);

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAccount(owner)).StatusCode);
        Assert.Empty((await staying.GetFromJsonAsync<List<RaceDto>>(Route(ApiEndpoints.Users.GetRaces), TestJson.Options))!);
    }

    [Fact]
    public async Task Join_EleventhAttemptInAMinute_Returns429()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);

        for (var i = 0; i < 10; i++)
        {
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await Join(client, "000000")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Join(client, "000000")).StatusCode);
    }

    private static Task<HttpResponseMessage> Join(HttpClient client, string pin) =>
        client.PostAsJsonAsync(Route(ApiEndpoints.Users.JoinRace), new JoinRaceRequest(pin), TestJson.Options);

    private static Task<HttpResponseMessage> DeleteAccount(HttpClient client) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, Route(ApiEndpoints.Users.DeleteMyUser))
        {
            Content = JsonContent.Create(new DeleteAccountRequest(Password))
        });

    private static async Task<(Guid RaceId, string Pin)> CreateRace(HttpClient owner)
    {
        var race = await Post<RaceDto>(owner, Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTCT", null));
        var detail = await owner.GetFromJsonAsync<RaceDetailDto>(RaceRoute(race.Id), TestJson.Options);
        return (race.Id, detail!.Pin!);
    }

    private static async Task<T> Post<T>(HttpClient client, string route, object body)
    {
        var response = await client.PostAsJsonAsync(route, body, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(TestJson.Options))!;
    }

    private static async Task<Guid> SignIn(HttpClient client)
    {
        var username = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username, Password));
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return (await client.GetFromJsonAsync<AppUserDto>(Route(ApiEndpoints.Users.GetMyUser)))!.Id;
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));

    private static string RaceRoute(Guid id) =>
        Route(ApiEndpoints.Users.GetRace.Replace("{id:guid}", id.ToString()));

    private static string PinRoute(Guid id) =>
        Route(ApiEndpoints.Users.RegenerateRacePin.Replace("{id:guid}", id.ToString()));

    private static string CrewRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.GetRaceCrew.Replace("{raceId:guid}", raceId.ToString()));

    private static string MemberRoute(Guid raceId, Guid memberId) =>
        Route(ApiEndpoints.Users.RemoveCrewMember
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{memberId:guid}", memberId.ToString()));

    private static string AidStationsRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.CreateAidStation.Replace("{raceId:guid}", raceId.ToString()));

    private static string SegmentsRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", raceId.ToString()));

    private static string ItemsRoute(Guid raceId, Guid segmentId) =>
        Route(ApiEndpoints.Users.CreateItem
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{segmentId:guid}", segmentId.ToString()));

    private static string Route(string route) => $"/{route}";
}
