using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteArrivalCommandHandlerTests
{
    private readonly RunScenario _s = new();

    private DeleteArrivalCommandHandler CreateHandler() => new(_s.RacesRepo, _s.RunsRepo, _s.Zone, _s.Clock);

    private Task<RaceRunStateDto> Delete(Guid userId, string checkpoint) =>
        CreateHandler().Handle(new DeleteArrivalCommand(userId, _s.RaceId, checkpoint), CancellationToken.None);

    [Fact]
    public async Task Handle_RemovesTheArrivalAndSaves()
    {
        _s.GivenRun();
        var as1 = _s.GivenArrival(_s.As1, 195);
        _s.GivenArrival(_s.As2, 320);

        var state = await Delete(_s.CrewId, _s.As1.Id.ToString());

        _s.RunsRepo.Received(1).DeleteArrival(as1);
        await _s.RunsRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal([_s.As2.Id.ToString()], state.Run!.Arrivals.Select(a => a.Checkpoint));
    }

    [Fact]
    public async Task Handle_UndoingTheFinish_ClearsFinishedAt()
    {
        var run = _s.GivenRun();
        _s.GivenArrival(null, 690);

        var state = await Delete(_s.CrewId, "finish");

        Assert.Null(run.FinishedAtUtc);
        Assert.Null(state.Run!.FinishedAtUtc);
        Assert.Empty(state.Run.Arrivals);
    }

    [Fact]
    public async Task Handle_ACheckpointThatWasNeverRecorded_ThrowsNotFound()
    {
        _s.GivenRun();

        await Assert.ThrowsAsync<NotFoundException>(() => Delete(_s.CrewId, _s.As1.Id.ToString()));
    }

    [Fact]
    public async Task Handle_AMalformedKey_ThrowsNotFound()
    {
        _s.GivenRun();

        await Assert.ThrowsAsync<NotFoundException>(() => Delete(_s.CrewId, "AS1"));
    }

    [Fact]
    public async Task Handle_BeforeTheRaceIsStarted_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Delete(_s.CrewId, "finish"));
    }

    [Fact]
    public async Task Handle_ForAStranger_ThrowsNotFoundAndDeletesNothing()
    {
        _s.GivenRun();
        _s.GivenArrival(_s.As1, 195);

        await Assert.ThrowsAsync<NotFoundException>(() => Delete(_s.StrangerId, _s.As1.Id.ToString()));

        _s.RunsRepo.DidNotReceive().DeleteArrival(Arg.Any<Arrivals>());
    }
}
