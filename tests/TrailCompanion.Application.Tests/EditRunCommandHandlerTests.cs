using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class EditRunCommandHandlerTests
{
    private readonly RunScenario _s = new();

    private Task<RaceRunStateDto> Edit(Guid userId, DateTimeOffset? startedAt) =>
        new EditRunCommandHandler(_s.RacesRepo, _s.RunsRepo, _s.Zone, _s.Clock)
            .Handle(new EditRunCommand(userId, _s.RaceId, new EditRunRequest(startedAt)), CancellationToken.None);

    private static string CodeOf(ValidationFailedException exception) => Assert.Single(exception.Failures).Code;

    [Fact]
    public async Task Handle_MovesTheStartAndSaves()
    {
        var run = _s.GivenRun();
        _s.Clock.Now = new DateTimeOffset(_s.Utc(30));

        var state = await Edit(_s.CrewId, new DateTimeOffset(_s.Utc(-10)));

        Assert.Equal(_s.Utc(-10), run.StartedAtUtc);
        Assert.Equal(_s.Utc(-10), state.Run!.StartedAtUtc);
        await _s.RunsRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StartAtOrAfterTheFirstArrival_FailsNamingIt()
    {
        var run = _s.GivenRun();
        _s.GivenArrival(_s.As1, 195);
        _s.Clock.Now = new DateTimeOffset(_s.Utc(300));

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Edit(_s.CrewId, new DateTimeOffset(_s.Utc(200))));

        Assert.Equal("StartAfterFirstArrival", CodeOf(exception));
        Assert.Equal(_s.Utc(0), run.StartedAtUtc);
        await _s.RunsRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InTheFuture_Fails()
    {
        _s.GivenRun();

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Edit(_s.CrewId, new DateTimeOffset(_s.Utc(10))));

        Assert.Equal("StartInFuture", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_WithoutAValue_Fails()
    {
        _s.GivenRun();

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Edit(_s.CrewId, null));

        Assert.Equal("StartedAtRequired", CodeOf(exception));
    }

    [Fact]
    public async Task Handle_WithNoRun_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Edit(_s.CrewId, new DateTimeOffset(_s.Utc(-5))));
    }

    [Fact]
    public async Task Handle_ForAStranger_ThrowsNotFoundBeforeLookingAtTheBody()
    {
        _s.GivenRun();

        await Assert.ThrowsAsync<NotFoundException>(() => Edit(_s.StrangerId, null));
    }
}
