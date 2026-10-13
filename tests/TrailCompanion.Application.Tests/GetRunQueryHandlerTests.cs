using TrailCompanion.Application.Modules.Users.Queries;
using TrailCompanion.Domain.Common.Exceptions;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class GetRunQueryHandlerTests
{
    private readonly RunScenario _s = new();

    private GetRunQueryHandler CreateHandler() => new(_s.RacesRepo, _s.RunsRepo, _s.Zone, _s.Clock);

    [Fact]
    public async Task Handle_BeforeTheStartOnRaceDay_ReturnsNoRunAndCanStart()
    {
        var state = await CreateHandler().Handle(new GetRunQuery(_s.CrewId, _s.RaceId), CancellationToken.None);

        Assert.Null(state.Run);
        Assert.True(state.CanStart);
        Assert.Equal(RunScenario.RaceDate, state.Today);
        Assert.Equal("Africa/Johannesburg", state.TimeZone);
    }

    [Fact]
    public async Task Handle_TheDayBeforeInJohannesburg_CannotStart()
    {
        _s.Clock.Now = new DateTimeOffset(2026, 10, 10, 21, 59, 59, TimeSpan.Zero);

        var state = await CreateHandler().Handle(new GetRunQuery(_s.OwnerId, _s.RaceId), CancellationToken.None);

        Assert.False(state.CanStart);
        Assert.Equal(new DateOnly(2026, 10, 10), state.Today);
    }

    [Fact]
    public async Task Handle_WithARun_ReturnsItWithArrivalsInTimeOrder()
    {
        var run = _s.GivenRun();
        _s.GivenArrival(_s.As2, 300);
        _s.GivenArrival(_s.As1, 195);

        var state = await CreateHandler().Handle(new GetRunQuery(_s.CrewId, _s.RaceId), CancellationToken.None);

        Assert.False(state.CanStart);
        Assert.Equal(run.Id, state.Run!.Id);
        Assert.Equal([_s.As1.Id.ToString(), _s.As2.Id.ToString()], state.Run.Arrivals.Select(a => a.Checkpoint));
    }

    [Fact]
    public async Task Handle_ForAStranger_ThrowsNotFoundAndReadsNoRun()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetRunQuery(_s.StrangerId, _s.RaceId), CancellationToken.None));

        await _s.RunsRepo.DidNotReceive().GetByRaceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
