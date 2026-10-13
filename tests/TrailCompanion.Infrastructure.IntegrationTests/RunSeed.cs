using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Infrastructure.IntegrationTests;

internal sealed record SeededRace(AppUser Owner, Races Race, Segments Segment, AidStations Station);

internal static class RunSeed
{
    public static async Task<AppUser> User(PostgresDatabaseFixture fixture)
    {
        var user = new AppUser { Username = Guid.NewGuid().ToString("N"), PasswordHash = "hash" };
        await using var context = fixture.CreateContext();
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<SeededRace> Race(PostgresDatabaseFixture fixture)
    {
        var owner = await User(fixture);
        var race = new Races { AppUserId = owner.Id, Name = "UTCT", Pin = TestPins.Next() };
        await using var context = fixture.CreateContext();
        context.Add(race);
        await context.SaveChangesAsync();

        var segment = new Segments { RaceId = race.Id, Position = 1, Name = "S1", Duration = 180 };
        context.Add(segment);
        await context.SaveChangesAsync();

        var station = new AidStations { SegmentId = segment.Id, Name = "AS1" };
        context.Add(station);
        await context.SaveChangesAsync();

        return new SeededRace(owner, race, segment, station);
    }

    public static async Task<RaceRuns> Run(
        PostgresDatabaseFixture fixture,
        Guid raceId,
        Guid? startedBy = null,
        DateTime? finishedAtUtc = null)
    {
        var run = new RaceRuns
        {
            RaceId = raceId,
            StartedAtUtc = DateTime.UtcNow,
            StartedBy = startedBy,
            FinishedAtUtc = finishedAtUtc
        };
        await using var context = fixture.CreateContext();
        context.Add(run);
        await context.SaveChangesAsync();
        return run;
    }

    public static async Task<Arrivals> Arrival(
        PostgresDatabaseFixture fixture,
        Guid runId,
        Guid? stationId,
        Guid? recordedBy = null,
        DateTime? at = null)
    {
        var arrival = new Arrivals
        {
            RaceRunId = runId,
            AidStationId = stationId,
            ArrivedAtUtc = at ?? DateTime.UtcNow,
            RecordedBy = recordedBy
        };
        await using var context = fixture.CreateContext();
        context.Add(arrival);
        await context.SaveChangesAsync();
        return arrival;
    }

    public static async Task Crew(PostgresDatabaseFixture fixture, Guid raceId, Guid appUserId)
    {
        await using var context = fixture.CreateContext();
        context.Add(new RaceCrewMembers { RaceId = raceId, AppUserId = appUserId });
        await context.SaveChangesAsync();
    }
}
