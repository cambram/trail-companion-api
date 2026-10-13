using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class RecordArrivalCommandHandlerTests
{
    private readonly RunScenario _s = new();

    private Task<RaceRunStateDto> Record(Guid userId, string? checkpoint, double? atMinutes = null) =>
        new RecordArrivalCommandHandler(_s.RacesRepo, _s.RunsRepo, _s.SegmentsRepo, _s.StationsRepo, _s.Zone, _s.Clock)
            .Handle(
                new RecordArrivalCommand(
                    userId,
                    _s.RaceId,
                    new RecordArrivalRequest(checkpoint, atMinutes is { } m ? new DateTimeOffset(_s.Utc(m)) : null)),
                CancellationToken.None);

    private void ClockAt(double minutesAfterStart) => _s.Clock.Now = new DateTimeOffset(_s.Utc(minutesAfterStart));

    private static string CodeOf(ValidationFailedException exception) => Assert.Single(exception.Failures).Code;

    [Fact]
    public async Task Handle_FirstStation_DefaultsToNowAndRecordsWhoTappedIt()
    {
        var run = _s.GivenRun();
        ClockAt(195);

        var state = await Record(_s.CrewId, _s.As1.Id.ToString());

        var arrival = Assert.Single(state.Run!.Arrivals);
        Assert.Equal(_s.As1.Id.ToString(), arrival.Checkpoint);
        Assert.Equal(_s.Utc(195), arrival.ArrivedAtUtc);
        Assert.Null(state.Run.FinishedAtUtc);
        await _s.RunsRepo.Received(1).AddArrivalAsync(
            Arg.Is<Arrivals>(a => a != null && a.RaceRunId == run.Id && a.AidStationId == _s.As1.Id && a.RecordedBy == _s.CrewId),
            Arg.Any<CancellationToken>());
        await _s.RunsRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TheOwnerCanRecordToo()
    {
        _s.GivenRun();
        ClockAt(195);

        var state = await Record(_s.OwnerId, _s.As1.Id.ToString());

        Assert.Single(state.Run!.Arrivals);
    }

    [Fact]
    public async Task Handle_Finish_SetsFinishedAt()
    {
        var run = _s.GivenRun();
        ClockAt(690);

        var state = await Record(_s.CrewId, "finish");

        Assert.Equal(_s.Utc(690), state.Run!.FinishedAtUtc);
        Assert.Equal(_s.Utc(690), run.FinishedAtUtc);
        Assert.Equal("finish", Assert.Single(state.Run.Arrivals).Checkpoint);
    }

    [Fact]
    public async Task Handle_AlreadyRecorded_ThrowsArrivalExistsAndStoresNothing()
    {
        _s.GivenRun();
        _s.GivenArrival(_s.As1, 195);
        ClockAt(200);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Record(_s.CrewId, _s.As1.Id.ToString()));

        Assert.Equal("ArrivalExists", exception.Code);
        await _s.RunsRepo.DidNotReceive().AddArrivalAsync(Arg.Any<Arrivals>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AMissedStationDoesNotBlockTheNextOne()
    {
        _s.GivenRun();
        ClockAt(320);

        var state = await Record(_s.CrewId, _s.As2.Id.ToString());

        Assert.Equal(_s.As2.Id.ToString(), Assert.Single(state.Run!.Arrivals).Checkpoint);
    }

    [Fact]
    public async Task Handle_BackfillingAMissedStationWithATimeBetweenItsNeighbours_Works()
    {
        _s.GivenRun();
        _s.GivenArrival(_s.As2, 320);
        ClockAt(400);

        var state = await Record(_s.CrewId, _s.As1.Id.ToString(), atMinutes: 200);

        Assert.Equal(
            [_s.As1.Id.ToString(), _s.As2.Id.ToString()],
            state.Run!.Arrivals.Select(a => a.Checkpoint));
    }

    [Fact]
    public async Task Handle_BackfillingWithTheDefaultTimeAfterALaterStation_FailsWithAClearMessage()
    {
        _s.GivenRun();
        _s.GivenArrival(_s.As2, 300);
        ClockAt(400);

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Record(_s.CrewId, _s.As1.Id.ToString()));

        Assert.Equal("ArrivalOutOfOrder", CodeOf(exception));
        Assert.Equal("AS1 must be before AS2 (11:00).", exception.Failures[0].Message);
    }

    [Fact]
    public async Task Handle_BeforeTheStart_Fails()
    {
        _s.GivenRun();
        ClockAt(100);

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Record(_s.CrewId, _s.As1.Id.ToString(), atMinutes: -5));

        Assert.Equal("ArrivalBeforeStart", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_InTheFuture_Fails()
    {
        _s.GivenRun();
        ClockAt(100);

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Record(_s.CrewId, _s.As1.Id.ToString(), atMinutes: 130));

        Assert.Equal("ArrivalInFuture", CodeOf(exception));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AS1")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public async Task Handle_UnknownCheckpoint_ThrowsInvalidCheckpoint(string? checkpoint)
    {
        _s.GivenRun();

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Record(_s.CrewId, checkpoint));

        Assert.Equal("InvalidCheckpoint", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_BeforeTheRaceIsStarted_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Record(_s.CrewId, "finish"));
    }

    [Fact]
    public async Task Handle_ForAStranger_ThrowsNotFoundBeforeLookingAtTheBody()
    {
        _s.GivenRun();

        await Assert.ThrowsAsync<NotFoundException>(() => Record(_s.StrangerId, null));

        await _s.RunsRepo.DidNotReceive().AddArrivalAsync(Arg.Any<Arrivals>(), Arg.Any<CancellationToken>());
    }
}
