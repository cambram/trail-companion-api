using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.Repositories.Users;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class RaceRunsRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public RaceRunsRepositoryTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByRaceIdAsync_ReturnsTheRunForOwnerAndCrewOnly()
    {
        var seed = await RunSeed.Race(_fixture);
        var run = await RunSeed.Run(_fixture, seed.Race.Id);
        var crew = await RunSeed.User(_fixture);
        var stranger = await RunSeed.User(_fixture);
        await RunSeed.Crew(_fixture, seed.Race.Id, crew.Id);

        await using var context = _fixture.CreateContext();
        var repository = new RaceRunsRepository(context);

        Assert.Equal(run.Id, (await repository.GetByRaceIdAsync(seed.Owner.Id, seed.Race.Id))!.Id);
        Assert.Equal(run.Id, (await repository.GetByRaceIdAsync(crew.Id, seed.Race.Id))!.Id);
        Assert.Null(await repository.GetByRaceIdAsync(stranger.Id, seed.Race.Id));
    }

    [Fact]
    public async Task GetByRaceIdAsync_AfterTheCrewMemberIsRemoved_ReturnsNull()
    {
        var seed = await RunSeed.Race(_fixture);
        await RunSeed.Run(_fixture, seed.Race.Id);
        var crew = await RunSeed.User(_fixture);
        await RunSeed.Crew(_fixture, seed.Race.Id, crew.Id);

        await using (var context = _fixture.CreateContext())
        {
            await context.Set<RaceCrewMembers>().Where(c => c.AppUserId == crew.Id).ExecuteDeleteAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            Assert.Null(await new RaceRunsRepository(context).GetByRaceIdAsync(crew.Id, seed.Race.Id));
        }
    }

    [Fact]
    public async Task GetByRaceIdAsync_ForCrewOfADifferentRace_ReturnsNull()
    {
        var raceA = await RunSeed.Race(_fixture);
        var raceB = await RunSeed.Race(_fixture);
        await RunSeed.Run(_fixture, raceB.Race.Id);
        var crew = await RunSeed.User(_fixture);
        await RunSeed.Crew(_fixture, raceA.Race.Id, crew.Id);

        await using var context = _fixture.CreateContext();

        Assert.Null(await new RaceRunsRepository(context).GetByRaceIdAsync(crew.Id, raceB.Race.Id));
    }

    [Fact]
    public async Task SaveChangesAsync_SecondRunForOneRace_ThrowsRunExists()
    {
        var seed = await RunSeed.Race(_fixture);
        await RunSeed.Run(_fixture, seed.Race.Id);

        await using var context = _fixture.CreateContext();
        var repository = new RaceRunsRepository(context);
        await repository.AddAsync(new RaceRuns { RaceId = seed.Race.Id, StartedAtUtc = DateTime.UtcNow });

        var exception = await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync());

        Assert.Equal("RunExists", exception.Code);
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateStationOrFinishArrival_ThrowsArrivalExists()
    {
        var seed = await RunSeed.Race(_fixture);
        var run = await RunSeed.Run(_fixture, seed.Race.Id);
        await RunSeed.Arrival(_fixture, run.Id, seed.Station.Id);
        await RunSeed.Arrival(_fixture, run.Id, null);

        foreach (var stationId in new Guid?[] { seed.Station.Id, null })
        {
            await using var context = _fixture.CreateContext();
            var repository = new RaceRunsRepository(context);
            await repository.AddArrivalAsync(new Arrivals
            {
                RaceRunId = run.Id,
                AidStationId = stationId,
                ArrivedAtUtc = DateTime.UtcNow
            });

            var exception = await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync());

            Assert.Equal("ArrivalExists", exception.Code);
        }
    }

    [Fact]
    public async Task HasActiveRunAsync_IsTrueOnlyForAnUnfinishedRun()
    {
        var none = await RunSeed.Race(_fixture);
        var active = await RunSeed.Race(_fixture);
        var finished = await RunSeed.Race(_fixture);
        await RunSeed.Run(_fixture, active.Race.Id);
        await RunSeed.Run(_fixture, finished.Race.Id, finishedAtUtc: DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        var repository = new RaceRunsRepository(context);

        Assert.False(await repository.HasActiveRunAsync(none.Race.Id));
        Assert.True(await repository.HasActiveRunAsync(active.Race.Id));
        Assert.False(await repository.HasActiveRunAsync(finished.Race.Id));
    }

    [Fact]
    public async Task GetArrivalsAsync_ReturnsArrivalsInTimeOrder()
    {
        var seed = await RunSeed.Race(_fixture);
        var run = await RunSeed.Run(_fixture, seed.Race.Id);
        var at = DateTime.UtcNow;
        await RunSeed.Arrival(_fixture, run.Id, null, at: at.AddHours(3));
        await RunSeed.Arrival(_fixture, run.Id, seed.Station.Id, at: at.AddHours(1));

        await using var context = _fixture.CreateContext();
        var arrivals = await new RaceRunsRepository(context).GetArrivalsAsync(run.Id);

        Assert.Equal([seed.Station.Id, null], arrivals.Select(a => a.AidStationId));
    }
}
