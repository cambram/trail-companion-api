using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class EditArrivalCommandHandlerTests
{
    private readonly RunScenario _s = new();

    private Task<RaceRunStateDto> Edit(Guid userId, string checkpoint, double? atMinutes) =>
        new EditArrivalCommandHandler(_s.RacesRepo, _s.RunsRepo, _s.SegmentsRepo, _s.StationsRepo, _s.Zone, _s.Clock)
            .Handle(
                new EditArrivalCommand(
                    userId,
                    _s.RaceId,
                    checkpoint,
                    new EditArrivalRequest(atMinutes is { } m ? new DateTimeOffset(_s.Utc(m)) : null)),
                CancellationToken.None);

    private void ClockAt(double minutesAfterStart) => _s.Clock.Now = new DateTimeOffset(_s.Utc(minutesAfterStart));

    private static string CodeOf(ValidationFailedException exception) => Assert.Single(exception.Failures).Code;

    [Fact]
    public async Task Handle_ChangesTheTimeAndSaves()
    {
        _s.GivenRun();
        var arrival = _s.GivenArrival(_s.As1, 195);
        ClockAt(300);

        var state = await Edit(_s.CrewId, _s.As1.Id.ToString(), 185);

        Assert.Equal(_s.Utc(185), arrival.ArrivedAtUtc);
        Assert.Equal(_s.Utc(185), Assert.Single(state.Run!.Arrivals).ArrivedAtUtc);
        await _s.RunsRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AnEditThatBreaksTheOrder_FailsAndChangesNothing()
    {
        _s.GivenRun();
        var as1 = _s.GivenArrival(_s.As1, 195);
        _s.GivenArrival(_s.As2, 320);
        ClockAt(400);

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Edit(_s.CrewId, _s.As1.Id.ToString(), 330));

        Assert.Equal("ArrivalOutOfOrder", CodeOf(exception));
        Assert.Equal(_s.Utc(195), as1.ArrivedAtUtc);
        await _s.RunsRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MovingAnArrivalNextToItsOwnOldTime_IsNotAConflictWithItself()
    {
        _s.GivenRun();
        _s.GivenArrival(_s.As1, 195);
        ClockAt(300);

        var state = await Edit(_s.CrewId, _s.As1.Id.ToString(), 196);

        Assert.Equal(_s.Utc(196), Assert.Single(state.Run!.Arrivals).ArrivedAtUtc);
    }

    [Fact]
    public async Task Handle_EditingTheFinish_MovesFinishedAt()
    {
        var run = _s.GivenRun();
        _s.GivenArrival(null, 690);
        ClockAt(700);

        var state = await Edit(_s.CrewId, "finish", 695);

        Assert.Equal(_s.Utc(695), run.FinishedAtUtc);
        Assert.Equal(_s.Utc(695), state.Run!.FinishedAtUtc);
    }

    [Fact]
    public async Task Handle_ACheckpointThatWasNeverRecorded_ThrowsNotFound()
    {
        _s.GivenRun();
        ClockAt(300);

        await Assert.ThrowsAsync<NotFoundException>(() => Edit(_s.CrewId, _s.As1.Id.ToString(), 195));
    }

    [Fact]
    public async Task Handle_AKeyThatIsNotOnTheRace_ThrowsNotFound()
    {
        _s.GivenRun();

        await Assert.ThrowsAsync<NotFoundException>(() => Edit(_s.CrewId, "AS1", 195));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Edit(_s.CrewId, "11111111-1111-1111-1111-111111111111", 195));
    }

    [Fact]
    public async Task Handle_WithoutATime_ThrowsArrivedAtRequired()
    {
        _s.GivenRun();
        _s.GivenArrival(_s.As1, 195);

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Edit(_s.CrewId, _s.As1.Id.ToString(), null));

        Assert.Equal("ArrivedAtRequired", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_ForAStranger_ThrowsNotFoundAndChangesNothing()
    {
        _s.GivenRun();
        var arrival = _s.GivenArrival(_s.As1, 195);

        await Assert.ThrowsAsync<NotFoundException>(() => Edit(_s.StrangerId, _s.As1.Id.ToString(), 185));

        Assert.Equal(_s.Utc(195), arrival.ArrivedAtUtc);
    }
}
