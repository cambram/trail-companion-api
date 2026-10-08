using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class RaceRunSchemaTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public RaceRunSchemaTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SecondRunForOneRace_ViolatesTheRaceIndex()
    {
        var seed = await RunSeed.Race(_fixture);
        await RunSeed.Run(_fixture, seed.Race.Id);

        var constraint = await ConstraintOf(() => RunSeed.Run(_fixture, seed.Race.Id));

        Assert.Equal("IX_RaceRuns_RaceId", constraint);
    }

    [Fact]
    public async Task SecondFinishArrival_AndSecondStationArrival_BothViolateTheArrivalIndex()
    {
        var seed = await RunSeed.Race(_fixture);
        var run = await RunSeed.Run(_fixture, seed.Race.Id);
        await RunSeed.Arrival(_fixture, run.Id, seed.Station.Id);
        await RunSeed.Arrival(_fixture, run.Id, null);

        Assert.Equal(
            "IX_Arrivals_RaceRunId_AidStationId",
            await ConstraintOf(() => RunSeed.Arrival(_fixture, run.Id, null)));
        Assert.Equal(
            "IX_Arrivals_RaceRunId_AidStationId",
            await ConstraintOf(() => RunSeed.Arrival(_fixture, run.Id, seed.Station.Id)));
    }

    [Fact]
    public async Task DeletingTheRace_RemovesItsRunAndArrivals()
    {
        var seed = await RunSeed.Race(_fixture);
        var run = await RunSeed.Run(_fixture, seed.Race.Id);
        await RunSeed.Arrival(_fixture, run.Id, seed.Station.Id);

        await using (var context = _fixture.CreateContext())
        {
            await context.Set<Races>().Where(r => r.Id == seed.Race.Id).ExecuteDeleteAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            Assert.False(await context.Set<RaceRuns>().AnyAsync(r => r.Id == run.Id));
            Assert.False(await context.Set<Arrivals>().AnyAsync(a => a.RaceRunId == run.Id));
        }
    }

    [Fact]
    public async Task DeletingTheAidStation_RemovesItsArrivalButKeepsTheFinish()
    {
        var seed = await RunSeed.Race(_fixture);
        var run = await RunSeed.Run(_fixture, seed.Race.Id);
        await RunSeed.Arrival(_fixture, run.Id, seed.Station.Id);
        await RunSeed.Arrival(_fixture, run.Id, null);

        await using (var context = _fixture.CreateContext())
        {
            await context.Set<AidStations>().Where(a => a.Id == seed.Station.Id).ExecuteDeleteAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var remaining = await context.Set<Arrivals>().Where(a => a.RaceRunId == run.Id).ToListAsync();
            Assert.Null(Assert.Single(remaining).AidStationId);
        }
    }

    [Fact]
    public async Task DeletingTheCrewAccountThatTappedEverything_KeepsTheRunAndNullsTheAuthor()
    {
        var seed = await RunSeed.Race(_fixture);
        var crew = await RunSeed.User(_fixture);
        var run = await RunSeed.Run(_fixture, seed.Race.Id, startedBy: crew.Id);
        var arrival = await RunSeed.Arrival(_fixture, run.Id, seed.Station.Id, recordedBy: crew.Id);

        await using (var context = _fixture.CreateContext())
        {
            await context.AppUsers.Where(u => u.Id == crew.Id).ExecuteDeleteAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var keptRun = await context.Set<RaceRuns>().SingleAsync(r => r.Id == run.Id);
            var keptArrival = await context.Set<Arrivals>().SingleAsync(a => a.Id == arrival.Id);
            Assert.Null(keptRun.StartedBy);
            Assert.Null(keptArrival.RecordedBy);
        }
    }

    private static async Task<string?> ConstraintOf(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(action);

        return ((PostgresException)exception.InnerException!).ConstraintName;
    }
}
