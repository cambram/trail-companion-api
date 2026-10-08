using TrailCompanion.Application.Modules.Users;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Tests;

public class RunRulesTests
{
    private readonly RunScenario _s = new();
    private readonly IReadOnlyList<Checkpoint> _checkpoints;

    public RunRulesTests()
    {
        _checkpoints = RunCheckpoints.Build(_s.SegmentRows, _s.StationRows);
    }

    private DateTime Start => _s.Utc(0);
    private DateTimeOffset Now(double minutesAfterStart) => _s.Utc(minutesAfterStart);

    private void Arrive(Checkpoint target, double minutes, double nowMinutes, params (int Index, double Minutes)[] recorded) =>
        RunRules.RequireArrival(
            _s.Zone,
            Start,
            _checkpoints,
            recorded.ToDictionary(r => r.Index, r => _s.Utc(r.Minutes)),
            target,
            _s.Utc(minutes),
            Now(nowMinutes));

    private static string CodeOf(ValidationFailedException exception) => Assert.Single(exception.Failures).Code;

    [Fact]
    public void ToUtc_TruncatesToMillisecondsAndKeepsUtcKind()
    {
        var instant = new DateTimeOffset(2026, 10, 11, 8, 0, 0, TimeSpan.FromHours(2)).AddTicks(1234567);

        var utc = RunRules.ToUtc(instant);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 10, 11, 6, 0, 0, DateTimeKind.Utc).AddTicks(1230000), utc);
    }

    [Fact]
    public void RequireArrival_BetweenNeighbours_Passes()
    {
        Arrive(_checkpoints[1], 300, 400, (0, 180), (2, 480));
    }

    [Fact]
    public void RequireArrival_WithNothingRecorded_PassesAfterTheStart()
    {
        Arrive(_checkpoints[0], 195, 200);
    }

    [Fact]
    public void RequireArrival_UpToOneMinuteAheadOfTheClock_Passes()
    {
        Arrive(_checkpoints[0], 200.5, 200);
    }

    [Fact]
    public void RequireArrival_InTheFuture_Fails()
    {
        var exception = Assert.Throws<ValidationFailedException>(() => Arrive(_checkpoints[0], 205, 200));

        Assert.Equal("ArrivalInFuture", CodeOf(exception));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RequireArrival_AtOrBeforeTheStart_FailsNamingTheStartTime(double minutes)
    {
        var exception = Assert.Throws<ValidationFailedException>(() => Arrive(_checkpoints[0], minutes, 200));

        Assert.Equal("ArrivalBeforeStart", CodeOf(exception));
        Assert.Contains("06:00", exception.Failures[0].Message);
    }

    [Fact]
    public void RequireArrival_NotAfterThePreviousRecordedCheckpoint_FailsNamingIt()
    {
        var exception = Assert.Throws<ValidationFailedException>(() => Arrive(_checkpoints[1], 180, 400, (0, 195)));

        Assert.Equal("ArrivalOutOfOrder", CodeOf(exception));
        Assert.Equal("AS2 must be after AS1 (09:15).", exception.Failures[0].Message);
    }

    [Fact]
    public void RequireArrival_NotBeforeTheNextRecordedCheckpoint_FailsNamingIt()
    {
        var exception = Assert.Throws<ValidationFailedException>(() => Arrive(_checkpoints[0], 320, 400, (1, 300)));

        Assert.Equal("ArrivalOutOfOrder", CodeOf(exception));
        Assert.Equal("AS1 must be before AS2 (11:00).", exception.Failures[0].Message);
    }

    [Fact]
    public void RequireArrival_SkipsMissingCheckpointsWhenLookingForNeighbours()
    {
        Arrive(_checkpoints[3], 700, 800, (0, 195), (2, 480));
    }

    private Races RaceOn(DateOnly? date) => new() { RaceDate = date, AppUserId = _s.OwnerId };

    [Fact]
    public void RequireStart_OnRaceDayNotInTheFuture_Passes()
    {
        RunRules.RequireStart(_s.Zone, RaceOn(RunScenario.RaceDate), _s.Utc(-60), Now(0), null);
    }

    [Fact]
    public void RequireStart_InTheFuture_Fails()
    {
        var exception = Assert.Throws<ValidationFailedException>(() =>
            RunRules.RequireStart(_s.Zone, RaceOn(RunScenario.RaceDate), _s.Utc(5), Now(0), null));

        Assert.Equal("StartInFuture", CodeOf(exception));
    }

    [Fact]
    public void RequireStart_OnAnotherJohannesburgDay_Fails()
    {
        var exception = Assert.Throws<ValidationFailedException>(() =>
            RunRules.RequireStart(_s.Zone, RaceOn(RunScenario.RaceDate), _s.Utc(-7 * 60), Now(0), null));

        Assert.Equal("StartNotOnRaceDay", CodeOf(exception));
        Assert.Contains("2026-10-11", exception.Failures[0].Message);
    }

    [Fact]
    public void RequireStart_WithoutARaceDate_Fails()
    {
        var exception = Assert.Throws<ValidationFailedException>(() =>
            RunRules.RequireStart(_s.Zone, RaceOn(null), _s.Utc(0), Now(0), null));

        Assert.Equal("RaceDateNotSet", CodeOf(exception));
    }

    [Fact]
    public void RequireStart_NotBeforeTheFirstArrival_FailsNamingIt()
    {
        var exception = Assert.Throws<ValidationFailedException>(() =>
            RunRules.RequireStart(_s.Zone, RaceOn(RunScenario.RaceDate), _s.Utc(200), Now(300), _s.Utc(195)));

        Assert.Equal("StartAfterFirstArrival", CodeOf(exception));
        Assert.Contains("09:15", exception.Failures[0].Message);
    }
}
