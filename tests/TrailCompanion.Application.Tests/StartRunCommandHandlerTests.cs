using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class StartRunCommandHandlerTests
{
    private readonly RunScenario _s = new();

    private Task<RaceRunStateDto> Start(Guid userId, DateTimeOffset? startedAt = null) =>
        new StartRunCommandHandler(_s.RacesRepo, _s.RunsRepo, _s.SegmentsRepo, _s.Zone, _s.Clock)
            .Handle(new StartRunCommand(userId, _s.RaceId, new StartRunRequest(startedAt)), CancellationToken.None);

    private static string CodeOf(ValidationFailedException exception) => Assert.Single(exception.Failures).Code;

    [Fact]
    public async Task Handle_OnRaceDay_CrewStartsTheRunNowAndIsRecordedAsStarter()
    {
        var state = await Start(_s.CrewId);

        Assert.Equal(_s.Clock.Now.UtcDateTime, state.Run!.StartedAtUtc);
        Assert.Null(state.Run.FinishedAtUtc);
        Assert.Empty(state.Run.Arrivals);
        Assert.False(state.CanStart);
        await _s.RunsRepo.Received(1).AddAsync(
            Arg.Is<RaceRuns>(r => r != null && r.RaceId == _s.RaceId && r.StartedBy == _s.CrewId),
            Arg.Any<CancellationToken>());
        await _s.RunsRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TheOwnerCanStartToo()
    {
        var state = await Start(_s.OwnerId);

        Assert.NotNull(state.Run);
    }

    [Theory]
    [InlineData("2026-10-10T21:59:59Z", false)]
    [InlineData("2026-10-10T22:00:00Z", true)]
    [InlineData("2026-10-11T21:59:59Z", true)]
    [InlineData("2026-10-11T22:00:00Z", false)]
    public async Task Handle_RaceDayIsTheJohannesburgDay_NotTheUtcDay(string now, bool allowed)
    {
        _s.Clock.Now = DateTimeOffset.Parse(now, System.Globalization.CultureInfo.InvariantCulture);

        if (allowed)
        {
            Assert.NotNull((await Start(_s.CrewId)).Run);
            return;
        }

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Start(_s.CrewId));
        Assert.Equal("NotRaceDay", CodeOf(exception));
        Assert.Contains("2026-10-11", exception.Failures[0].Message);
        await _s.RunsRepo.DidNotReceive().AddAsync(Arg.Any<RaceRuns>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenARunExists_ThrowsRunExistsEvenOnTheWrongDay()
    {
        _s.GivenRun();
        _s.Clock.Now = new DateTimeOffset(2026, 10, 13, 8, 0, 0, TimeSpan.Zero);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Start(_s.CrewId));

        Assert.Equal("RunExists", exception.Code);
        await _s.RunsRepo.DidNotReceive().AddAsync(Arg.Any<RaceRuns>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutARaceDate_ThrowsRaceDateNotSet()
    {
        var s = new RunScenario();
        s.Race.RaceDate = null;

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            new StartRunCommandHandler(s.RacesRepo, s.RunsRepo, s.SegmentsRepo, s.Zone, s.Clock)
                .Handle(new StartRunCommand(s.CrewId, s.RaceId, new StartRunRequest(null)), CancellationToken.None));

        Assert.Equal("RaceDateNotSet", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_WithNoSegments_ThrowsNoSegments()
    {
        _s.SegmentsRepo.GetByRaceIdAsync(_s.OwnerId, _s.RaceId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Segments>());

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Start(_s.CrewId));

        Assert.Equal("NoSegments", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_WithAnEarlierStartOnRaceDay_UsesIt()
    {
        var gun = new DateTimeOffset(_s.Utc(-10));

        var state = await Start(_s.CrewId, gun);

        Assert.Equal(gun.UtcDateTime, state.Run!.StartedAtUtc);
    }

    [Fact]
    public async Task Handle_WithAStartInTheFuture_Fails()
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Start(_s.CrewId, new DateTimeOffset(_s.Utc(10))));

        Assert.Equal("StartInFuture", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_WithAStartOnAnotherDay_Fails()
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Start(_s.CrewId, new DateTimeOffset(_s.Utc(-7 * 60))));

        Assert.Equal("StartNotOnRaceDay", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_ForAStranger_ThrowsNotFoundAndWritesNothing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Start(_s.StrangerId));

        await _s.RunsRepo.DidNotReceive().AddAsync(Arg.Any<RaceRuns>(), Arg.Any<CancellationToken>());
        await _s.RunsRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
