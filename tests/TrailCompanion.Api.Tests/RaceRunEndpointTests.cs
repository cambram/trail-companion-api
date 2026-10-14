using System.Net;
using System.Net.Http.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class RaceRunEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public RaceRunEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Start_IsBlockedUntilRaceDayInJohannesburg_AndAgainOnceItHasPassed()
    {
        using var host = new RunHost(_fixture, RunHost.Utc("2026-10-10T21:30:00Z"));
        var (owner, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);

        var dayBefore = await host.State(owner, plan.RaceId);
        Assert.Equal(new DateOnly(2026, 10, 10), dayBefore.Today);
        Assert.False(dayBefore.CanStart);
        Assert.Null(dayBefore.Run);
        Assert.Equal("Africa/Johannesburg", dayBefore.TimeZone);

        var blocked = await host.Send(owner, HttpMethod.Post, RunHost.StartRoute(plan.RaceId), new StartRunRequest(null));
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("NotRaceDay", await RunHost.ErrorCode(blocked));

        host.Clock.Now = RunHost.Utc("2026-10-10T22:00:00Z");
        var open = await host.State(owner, plan.RaceId);
        Assert.True(open.CanStart);
        Assert.Equal(RunHost.RaceDate, open.Today);

        host.Clock.Now = RunHost.Utc("2026-10-11T22:30:00Z");
        var over = await host.State(owner, plan.RaceId);
        Assert.False(over.CanStart);
        var late = await host.Send(owner, HttpMethod.Post, RunHost.StartRoute(plan.RaceId), new StartRunRequest(null));
        Assert.Equal(HttpStatusCode.BadRequest, late.StatusCode);
        Assert.Equal("NotRaceDay", await RunHost.ErrorCode(late));

        host.At(0);
        var started = await host.Start(owner, plan.RaceId);
        Assert.Equal(host.Moment(0).UtcDateTime, started.Run!.StartedAtUtc);
        Assert.False(started.CanStart);
    }

    [Fact]
    public async Task TwoCrewOnSeparateDevices_SeeEachOthersTapsThroughAFullRun()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var (crewA, _) = await host.NewUser();
        var (crewB, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        await host.Join(crewA, plan.Pin);
        await host.Join(crewB, plan.Pin);

        await host.Start(crewA, plan.RaceId);
        Assert.NotNull((await host.State(crewB, plan.RaceId)).Run);

        host.At(195);
        await host.Record(crewB, plan.RaceId, plan.StationIds[0].ToString());
        Assert.Equal(
            [plan.StationIds[0].ToString()],
            (await host.State(crewA, plan.RaceId)).Run!.Arrivals.Select(a => a.Checkpoint));

        host.At(320);
        await host.Record(crewA, plan.RaceId, plan.StationIds[1].ToString());
        host.At(500);
        await host.Record(crewB, plan.RaceId, plan.StationIds[2].ToString());
        host.At(690);
        await host.Record(crewA, plan.RaceId, "finish");

        var seenByOwner = await host.State(owner, plan.RaceId);
        Assert.Equal(4, seenByOwner.Run!.Arrivals.Count);
        Assert.Equal(host.Moment(690).UtcDateTime, seenByOwner.Run.FinishedAtUtc);
        Assert.Equal("finish", seenByOwner.Run.Arrivals[^1].Checkpoint);
    }

    [Fact]
    public async Task Crew_CanStartRecordCorrectBackfillAndUndoEveryStep()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var (crew, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        await host.Join(crew, plan.Pin);
        var as1 = plan.StationIds[0].ToString();
        var as2 = plan.StationIds[1].ToString();

        await host.Start(crew, plan.RaceId);

        var movedStart = await host.Ok(host.Send(
            crew, HttpMethod.Patch, RunHost.RunRoute(plan.RaceId), new EditRunRequest(host.Moment(-10))));
        Assert.Equal(host.Moment(-10).UtcDateTime, movedStart.Run!.StartedAtUtc);

        host.At(195);
        await host.Record(crew, plan.RaceId, as1);
        var edited = await host.Ok(host.Send(
            crew, HttpMethod.Patch, RunHost.ArrivalRoute(plan.RaceId, as1), new EditArrivalRequest(host.Moment(185))));
        Assert.Equal(host.Moment(185).UtcDateTime, Assert.Single(edited.Run!.Arrivals).ArrivedAtUtc);

        var undone = await host.Ok(host.Send(crew, HttpMethod.Delete, RunHost.ArrivalRoute(plan.RaceId, as1)));
        Assert.Empty(undone.Run!.Arrivals);

        host.At(320);
        await host.Record(crew, plan.RaceId, as2);
        var backfilled = await host.Record(crew, plan.RaceId, as1, host.Moment(200));
        Assert.Equal([as1, as2], backfilled.Run!.Arrivals.Select(a => a.Checkpoint));

        host.At(400);
        var outOfOrder = await host.Send(
            crew, HttpMethod.Patch, RunHost.ArrivalRoute(plan.RaceId, as1), new EditArrivalRequest(host.Moment(330)));
        Assert.Equal(HttpStatusCode.BadRequest, outOfOrder.StatusCode);
        Assert.Equal("ArrivalOutOfOrder", await RunHost.ErrorCode(outOfOrder));
        Assert.Equal("AS1 must be before AS2 (11:20).", await RunHost.ErrorMessage(outOfOrder));

        var startAfterArrival = await host.Send(
            crew, HttpMethod.Patch, RunHost.RunRoute(plan.RaceId), new EditRunRequest(host.Moment(250)));
        Assert.Equal("StartAfterFirstArrival", await RunHost.ErrorCode(startAfterArrival));

        host.At(690);
        var finished = await host.Record(crew, plan.RaceId, "finish");
        Assert.NotNull(finished.Run!.FinishedAtUtc);

        var reopened = await host.Ok(host.Send(crew, HttpMethod.Delete, RunHost.ArrivalRoute(plan.RaceId, "finish")));
        Assert.Null(reopened.Run!.FinishedAtUtc);

        var startUndone = await host.Ok(host.Send(crew, HttpMethod.Delete, RunHost.RunRoute(plan.RaceId)));
        Assert.Null(startUndone.Run);
        Assert.True(startUndone.CanStart);
        Assert.Null((await host.State(owner, plan.RaceId)).Run);
    }

    [Fact]
    public async Task RecordingValidation_GivesClearMessages()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        var as1 = plan.StationIds[0].ToString();
        await host.Start(owner, plan.RaceId);
        host.At(100);

        async Task<(HttpStatusCode Status, string Code)> Try(string? checkpoint, DateTimeOffset? at)
        {
            var response = await host.Send(
                owner, HttpMethod.Post, RunHost.ArrivalsRoute(plan.RaceId), new RecordArrivalRequest(checkpoint, at));

            return (response.StatusCode, await RunHost.ErrorCode(response));
        }

        Assert.Equal((HttpStatusCode.BadRequest, "InvalidCheckpoint"), await Try(Guid.NewGuid().ToString(), null));
        Assert.Equal((HttpStatusCode.BadRequest, "InvalidCheckpoint"), await Try(null, null));
        Assert.Equal((HttpStatusCode.BadRequest, "ArrivalBeforeStart"), await Try(as1, host.Moment(-5)));
        Assert.Equal((HttpStatusCode.BadRequest, "ArrivalInFuture"), await Try(as1, host.Moment(130)));
    }

    [Fact]
    public async Task TwoTapsOnTheSameCheckpoint_StoreOneArrival_AndTheLoserGets409()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var (crewA, _) = await host.NewUser();
        var (crewB, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        await host.Join(crewA, plan.Pin);
        await host.Join(crewB, plan.Pin);
        await host.Start(owner, plan.RaceId);
        host.At(195);
        var as1 = plan.StationIds[0].ToString();

        var responses = await Task.WhenAll(
            host.Send(crewA, HttpMethod.Post, RunHost.ArrivalsRoute(plan.RaceId), new RecordArrivalRequest(as1, null)),
            host.Send(crewB, HttpMethod.Post, RunHost.ArrivalsRoute(plan.RaceId), new RecordArrivalRequest(as1, null)));

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict],
            responses.Select(r => r.StatusCode).OrderBy(code => code));
        var loser = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("ArrivalExists", await RunHost.ErrorCode(loser));
        Assert.Single((await host.State(owner, plan.RaceId)).Run!.Arrivals);
    }

    [Fact]
    public async Task StrangersRemovedCrewAndCrewOfAnotherRace_Get404OnEveryRunEndpoint()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var (removed, removedId) = await host.NewUser();
        var (stranger, _) = await host.NewUser();
        var (otherCrew, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        var otherRace = await host.CreateUtctRace(owner);
        await host.Join(removed, plan.Pin);
        await host.Join(otherCrew, otherRace.Pin);
        await host.Start(owner, plan.RaceId);
        host.At(195);
        await host.Record(owner, plan.RaceId, plan.StationIds[0].ToString());

        Assert.NotNull((await host.State(removed, plan.RaceId)).Run);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await removedFromRace()).StatusCode);

        foreach (var (name, client) in new[] { ("removed crew", removed), ("stranger", stranger), ("crew of another race", otherCrew) })
        {
            foreach (var (method, route, body) in RunRequests(host, plan))
            {
                var response = await host.Send(client, method, route, body);

                Assert.True(
                    response.StatusCode == HttpStatusCode.NotFound,
                    $"{name}: {method} {route} returned {(int)response.StatusCode}");
            }
        }

        var intact = await host.State(owner, plan.RaceId);
        Assert.Single(intact.Run!.Arrivals);

        Task<HttpResponseMessage> removedFromRace() =>
            owner.DeleteAsync(RunHost.MemberRoute(plan.RaceId, removedId));
    }

    [Fact]
    public async Task Crew_StillGet404_OnEveryPlanEditingWrite_EvenWhileARunIsActive()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var (crew, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        await host.Join(crew, plan.Pin);

        var gel = await host.Post<NutritionItemDto>(
            owner,
            RunHost.Route(ApiEndpoints.Users.CreateNutritionItem),
            new NutritionItemRequest("Gel", NutritionType.Liquid, null, 40, null, null, null, null, null, null));
        var segmentId = plan.SegmentIds[0];
        var stationId = plan.StationIds[0];
        var item = await host.Post<ItemDto>(
            owner,
            RunHost.Route(ApiEndpoints.Users.CreateItem
                .Replace("{raceId:guid}", plan.RaceId.ToString())
                .Replace("{segmentId:guid}", segmentId.ToString())),
            new ItemRequest(gel.Id));
        var stationItem = await host.Post<AidStationItemDto>(
            owner,
            RunHost.Route(ApiEndpoints.Users.CreateAidStationItem
                .Replace("{raceId:guid}", plan.RaceId.ToString())
                .Replace("{aidStationId:guid}", stationId.ToString())),
            new ItemRequest(gel.Id));
        await host.Start(owner, plan.RaceId);

        string Path(string template, params (string Name, Guid Value)[] values)
        {
            var path = template.Replace("{raceId:guid}", plan.RaceId.ToString()).Replace("{id:guid}", plan.RaceId.ToString());

            foreach (var (name, value) in values)
            {
                path = path.Replace($"{{{name}:guid}}", value.ToString());
            }

            return RunHost.Route(path);
        }

        var writes = new (HttpMethod Method, string Route, object? Body)[]
        {
            (HttpMethod.Put, Path(ApiEndpoints.Users.UpdateRace), new CreateRaceRequest("Hijacked", 1)),
            (HttpMethod.Delete, Path(ApiEndpoints.Users.DeleteRace), null),
            (HttpMethod.Post, Path(ApiEndpoints.Users.RegenerateRacePin), null),
            (HttpMethod.Post, Path(ApiEndpoints.Users.CreateSegment), new SegmentRequest("X", 10, null, null)),
            (HttpMethod.Put, Path(ApiEndpoints.Users.ReorderSegments), new ReorderRequest(plan.SegmentIds.Reverse().ToList())),
            (HttpMethod.Put, Path(ApiEndpoints.Users.UpdateSegment.Replace("{id:guid}", segmentId.ToString())), new SegmentRequest("X", 10, null, null)),
            (HttpMethod.Delete, Path(ApiEndpoints.Users.DeleteSegment.Replace("{id:guid}", segmentId.ToString())), null),
            (HttpMethod.Post, Path(ApiEndpoints.Users.CreateItem, ("segmentId", segmentId)), new ItemRequest(gel.Id)),
            (HttpMethod.Put, Path(ApiEndpoints.Users.ReorderItems, ("segmentId", segmentId)), new ReorderRequest([item.Id])),
            (HttpMethod.Delete, Path(ApiEndpoints.Users.DeleteItem.Replace("{id:guid}", item.Id.ToString()), ("segmentId", segmentId)), null),
            (HttpMethod.Post, Path(ApiEndpoints.Users.CreateAidStation), new CreateAidStationRequest(segmentId, "X")),
            (HttpMethod.Put, Path(ApiEndpoints.Users.UpdateAidStation.Replace("{id:guid}", stationId.ToString())), new AidStationRequest("X", null, null)),
            (HttpMethod.Delete, Path(ApiEndpoints.Users.DeleteAidStation.Replace("{id:guid}", stationId.ToString())), null),
            (HttpMethod.Put, Path(ApiEndpoints.Users.MoveAidStation.Replace("{id:guid}", stationId.ToString())), new MoveAidStationRequest(plan.SegmentIds[3])),
            (HttpMethod.Post, Path(ApiEndpoints.Users.CreateAidStationItem, ("aidStationId", stationId)), new ItemRequest(gel.Id)),
            (HttpMethod.Put, Path(ApiEndpoints.Users.ReorderAidStationItems, ("aidStationId", stationId)), new ReorderRequest([stationItem.Id])),
            (HttpMethod.Delete, Path(ApiEndpoints.Users.DeleteAidStationItem.Replace("{id:guid}", stationItem.Id.ToString()), ("aidStationId", stationId)), null)
        };

        foreach (var (method, route, body) in writes)
        {
            var response = await host.Send(crew, method, route, body);

            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound,
                $"crew {method} {route} returned {(int)response.StatusCode}");
        }

        var after = await owner.GetFromJsonAsync<RaceDetailDto>(RunHost.RaceRoute(plan.RaceId), TestJson.Options);
        Assert.Equal("UTCT 55K", after!.Name);
        Assert.Equal(4, after.Segments.Count);
        Assert.Equal(3, after.Segments.Count(s => s.AidStation is not null));
        Assert.Single(after.Segments[0].Items);
        Assert.Single(after.Segments[1].AidStation!.Items);
        Assert.NotNull((await host.State(owner, plan.RaceId)).Run);
    }

    [Fact]
    public async Task StructuralEdits_AreBlockedWhileTheRunIsActive_ButOtherEditsStillWork()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        var raceId = plan.RaceId.ToString();
        await host.Start(owner, plan.RaceId);

        string Station(Guid id, string template) =>
            RunHost.Route(template.Replace("{raceId:guid}", raceId).Replace("{id:guid}", id.ToString()));

        async Task AssertRunInProgress(HttpMethod method, string route, object? body)
        {
            var response = await host.Send(owner, method, route, body);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("RunInProgress", await RunHost.ErrorCode(response));
        }

        await AssertRunInProgress(HttpMethod.Delete, Station(plan.StationIds[0], ApiEndpoints.Users.DeleteAidStation), null);
        await AssertRunInProgress(
            HttpMethod.Delete, Station(plan.SegmentIds[1], ApiEndpoints.Users.DeleteSegment), null);
        await AssertRunInProgress(
            HttpMethod.Put,
            Station(plan.StationIds[0], ApiEndpoints.Users.MoveAidStation),
            new MoveAidStationRequest(plan.SegmentIds[3]));
        await AssertRunInProgress(
            HttpMethod.Put,
            RunHost.Route(ApiEndpoints.Users.ReorderSegments.Replace("{raceId:guid}", raceId)),
            new ReorderRequest(plan.SegmentIds.Reverse().ToList()));
        await AssertRunInProgress(
            HttpMethod.Post,
            RunHost.Route(ApiEndpoints.Users.CreateAidStation.Replace("{raceId:guid}", raceId)),
            new CreateAidStationRequest(plan.SegmentIds[0], "Extra"));

        var duration = await host.Send(
            owner,
            HttpMethod.Put,
            Station(plan.SegmentIds[0], ApiEndpoints.Users.UpdateSegment),
            new SegmentRequest("S1", 200, 18, null));
        Assert.Equal(HttpStatusCode.OK, duration.StatusCode);
        var rename = await host.Send(
            owner,
            HttpMethod.Put,
            Station(plan.StationIds[0], ApiEndpoints.Users.UpdateAidStation),
            new AidStationRequest("Renamed", null, null));
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);

        host.At(690);
        await host.Record(owner, plan.RaceId, "finish");
        var afterFinish = await host.Send(
            owner, HttpMethod.Delete, Station(plan.StationIds[2], ApiEndpoints.Users.DeleteAidStation));
        Assert.Equal(HttpStatusCode.NoContent, afterFinish.StatusCode);

        await host.Ok(host.Send(owner, HttpMethod.Delete, RunHost.ArrivalRoute(plan.RaceId, "finish")));
        await AssertRunInProgress(HttpMethod.Delete, Station(plan.StationIds[0], ApiEndpoints.Users.DeleteAidStation), null);
    }

    [Fact]
    public async Task DeletingTheCrewAccountThatStartedTheRun_KeepsTheRunAndItsArrivals()
    {
        using var host = new RunHost(_fixture, RunHost.RaceMorning);
        var (owner, _) = await host.NewUser();
        var (crew, _) = await host.NewUser();
        var plan = await host.CreateUtctRace(owner);
        await host.Join(crew, plan.Pin);
        await host.Start(crew, plan.RaceId);
        host.At(195);
        await host.Record(crew, plan.RaceId, plan.StationIds[0].ToString());

        var deleted = await crew.SendAsync(new HttpRequestMessage(HttpMethod.Delete, RunHost.Route(ApiEndpoints.Users.DeleteMyUser))
        {
            Content = JsonContent.Create(new DeleteAccountRequest(RunHost.Password))
        });
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var seenByOwner = await host.State(owner, plan.RaceId);
        var runId = seenByOwner.Run!.Id;
        Assert.Single(seenByOwner.Run.Arrivals);

        await using var context = _fixture.CreateContext();
        var startedBy = await context.Set<RaceRuns>()
            .Where(r => r.RaceId == plan.RaceId)
            .Select(r => r.StartedBy)
            .SingleAsync();
        var recordedBy = await context.Set<Arrivals>()
            .Where(a => a.RaceRunId == runId)
            .Select(a => a.RecordedBy)
            .SingleAsync();
        Assert.Null(startedBy);
        Assert.Null(recordedBy);
    }

    private static IEnumerable<(HttpMethod Method, string Route, object? Body)> RunRequests(RunHost host, UtctPlan plan)
    {
        var as1 = plan.StationIds[0].ToString();

        return
        [
            (HttpMethod.Get, RunHost.RunRoute(plan.RaceId), null),
            (HttpMethod.Post, RunHost.StartRoute(plan.RaceId), new StartRunRequest(null)),
            (HttpMethod.Patch, RunHost.RunRoute(plan.RaceId), new EditRunRequest(host.Moment(-5))),
            (HttpMethod.Post, RunHost.ArrivalsRoute(plan.RaceId), new RecordArrivalRequest("finish", null)),
            (HttpMethod.Patch, RunHost.ArrivalRoute(plan.RaceId, as1), new EditArrivalRequest(host.Moment(190))),
            (HttpMethod.Delete, RunHost.ArrivalRoute(plan.RaceId, as1), null),
            (HttpMethod.Delete, RunHost.RunRoute(plan.RaceId), null)
        ];
    }
}
