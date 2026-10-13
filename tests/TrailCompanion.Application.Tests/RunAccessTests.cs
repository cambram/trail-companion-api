using TrailCompanion.Application.Modules.Users;
using TrailCompanion.Domain.Common.Exceptions;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class RunAccessTests
{
    private readonly RunScenario _s = new();

    [Fact]
    public async Task RequireAsync_ForOwnerAndCrew_ReturnsTheRace()
    {
        Assert.Same(_s.Race, await RaceRunAccess.RequireAsync(_s.RacesRepo, _s.OwnerId, _s.RaceId, CancellationToken.None));
        Assert.Same(_s.Race, await RaceRunAccess.RequireAsync(_s.RacesRepo, _s.CrewId, _s.RaceId, CancellationToken.None));
    }

    [Fact]
    public async Task RequireAsync_ForAStrangerOrRemovedCrew_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            RaceRunAccess.RequireAsync(_s.RacesRepo, _s.StrangerId, _s.RaceId, CancellationToken.None));
    }

    [Fact]
    public async Task RunGuard_WithAnActiveRun_ThrowsRunInProgress()
    {
        _s.RunsRepo.HasActiveRunAsync(_s.RaceId, Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            RunGuard.RequireNoActiveRunAsync(_s.RunsRepo, _s.RaceId, CancellationToken.None));

        Assert.Equal("RunInProgress", exception.Code);
    }

    [Fact]
    public async Task RunGuard_WithNoActiveRun_Passes()
    {
        _s.RunsRepo.HasActiveRunAsync(_s.RaceId, Arg.Any<CancellationToken>()).Returns(false);

        await RunGuard.RequireNoActiveRunAsync(_s.RunsRepo, _s.RaceId, CancellationToken.None);
    }
}
