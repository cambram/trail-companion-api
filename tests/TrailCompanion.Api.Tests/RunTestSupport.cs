using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Api.Tests;

internal sealed class ManualClock : TimeProvider
{
    public ManualClock(DateTimeOffset now)
    {
        Now = now;
    }

    public DateTimeOffset Now { get; set; }

    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed record UtctPlan(
    Guid RaceId,
    string Pin,
    IReadOnlyList<Guid> SegmentIds,
    IReadOnlyList<Guid> StationIds);

internal sealed class RunHost : IDisposable
{
    public const string Password = "correct horse battery";
    public static readonly DateTimeOffset RaceMorning = new(2026, 10, 11, 4, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly RaceDate = new(2026, 10, 11);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<HttpClient> _clients = [];

    public RunHost(PostgresDatabaseFixture fixture, DateTimeOffset now)
    {
        Clock = new ManualClock(now);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long")
            .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock)));
    }

    public ManualClock Clock { get; }

    public static DateTimeOffset Utc(string iso) =>
        DateTimeOffset.Parse(iso, System.Globalization.CultureInfo.InvariantCulture);

    public DateTimeOffset Moment(double minutesAfterMorning) => RaceMorning.AddMinutes(minutesAfterMorning);

    public void At(double minutesAfterMorning) => Clock.Now = Moment(minutesAfterMorning);

    public async Task<(HttpClient Client, Guid Id)> NewUser()
    {
        var client = _factory.CreateClient();
        _clients.Add(client);
        var username = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username, Password));
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var me = await client.GetFromJsonAsync<AppUserDto>(Route(ApiEndpoints.Users.GetMyUser));

        return (client, me!.Id);
    }

    public async Task<UtctPlan> CreateUtctRace(HttpClient owner, DateOnly? raceDate = null)
    {
        var race = await Post<RaceDto>(
            owner,
            Route(ApiEndpoints.Users.CreateRace),
            new CreateRaceRequest("UTCT 55K", 70, new TimeOnly(6, 0), raceDate ?? RaceDate));

        int[] durations = [180, 120, 180, 180];
        double[] marks = [18, 30, 44, 55];
        var segmentIds = new List<Guid>();

        for (var i = 0; i < durations.Length; i++)
        {
            var segment = await Post<SegmentDto>(
                owner,
                Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", race.Id.ToString())),
                new SegmentRequest($"S{i + 1}", durations[i], marks[i], null));
            segmentIds.Add(segment.Id);
        }

        var stationIds = new List<Guid>();

        for (var i = 1; i < segmentIds.Count; i++)
        {
            var station = await Post<AidStationDto>(
                owner,
                Route(ApiEndpoints.Users.CreateAidStation.Replace("{raceId:guid}", race.Id.ToString())),
                new CreateAidStationRequest(segmentIds[i], $"AS{i}"));
            stationIds.Add(station.Id);
        }

        var detail = await owner.GetFromJsonAsync<RaceDetailDto>(RaceRoute(race.Id), TestJson.Options);

        return new UtctPlan(race.Id, detail!.Pin!, segmentIds, stationIds);
    }

    public async Task Join(HttpClient client, string pin)
    {
        var response = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.JoinRace), new JoinRaceRequest(pin), TestJson.Options);
        response.EnsureSuccessStatusCode();
    }

    public Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string route, object? body = null)
    {
        var request = new HttpRequestMessage(method, route);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: TestJson.Options);
        }

        return client.SendAsync(request);
    }

    public async Task<T> Post<T>(HttpClient client, string route, object body)
    {
        var response = await client.PostAsJsonAsync(route, body, TestJson.Options);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>(TestJson.Options))!;
    }

    public async Task<RaceRunStateDto> Ok(Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<RaceRunStateDto>(TestJson.Options))!;
    }

    public Task<RaceRunStateDto> State(HttpClient client, Guid raceId) =>
        Ok(Send(client, HttpMethod.Get, RunRoute(raceId)));

    public Task<RaceRunStateDto> Start(HttpClient client, Guid raceId, DateTimeOffset? at = null) =>
        Ok(Send(client, HttpMethod.Post, StartRoute(raceId), new StartRunRequest(at)));

    public Task<RaceRunStateDto> Record(HttpClient client, Guid raceId, string checkpoint, DateTimeOffset? at = null) =>
        Ok(Send(client, HttpMethod.Post, ArrivalsRoute(raceId), new RecordArrivalRequest(checkpoint, at)));

    public static async Task<string> ErrorCode(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("validationErrors")[0].GetProperty("code").GetString()!;
    }

    public static async Task<string> ErrorMessage(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("validationErrors")[0].GetProperty("message").GetString()!;
    }

    public static string RaceRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.GetRace.Replace("{id:guid}", raceId.ToString()));

    public static string RunRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.GetRun.Replace("{raceId:guid}", raceId.ToString()));

    public static string StartRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.StartRun.Replace("{raceId:guid}", raceId.ToString()));

    public static string ArrivalsRoute(Guid raceId) =>
        Route(ApiEndpoints.Users.RecordArrival.Replace("{raceId:guid}", raceId.ToString()));

    public static string ArrivalRoute(Guid raceId, string checkpoint) =>
        Route(ApiEndpoints.Users.EditArrival.Replace("{raceId:guid}", raceId.ToString()).Replace("{checkpoint}", checkpoint));

    public static string MemberRoute(Guid raceId, Guid memberId) =>
        Route(ApiEndpoints.Users.RemoveCrewMember
            .Replace("{raceId:guid}", raceId.ToString())
            .Replace("{memberId:guid}", memberId.ToString()));

    public static string Route(string route) => $"/{route}";

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }

        _factory.Dispose();
    }
}
