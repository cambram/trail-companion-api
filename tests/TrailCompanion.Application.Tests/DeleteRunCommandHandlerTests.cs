using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteRunCommandHandlerTests
{
    private readonly RunScenario _s = new();

    private DeleteRunCommandHandler CreateHandler() => new(_s.RacesRepo, _s.RunsRepo, _s.Zone, _s.Clock);

    [Fact]
    public async Task Handle_DeletesTheRunAndReturnsAFreshStartableState()
    {
        var run = _s.GivenRun();
        _s.GivenArrival(_s.As1, 195);

        var state = await CreateHandler().Handle(new DeleteRunCommand(_s.CrewId, _s.RaceId), CancellationToken.None);

        _s.RunsRepo.Received(1).Delete(run);
        await _s.RunsRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Null(state.Run);
        Assert.True(state.CanStart);
    }

    [Fact]
    public async Task Handle_WithNoRun_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteRunCommand(_s.CrewId, _s.RaceId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForAStranger_ThrowsNotFoundAndDeletesNothing()
    {
        _s.GivenRun();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteRunCommand(_s.StrangerId, _s.RaceId), CancellationToken.None));

        _s.RunsRepo.DidNotReceive().Delete(Arg.Any<RaceRuns>());
    }
}
