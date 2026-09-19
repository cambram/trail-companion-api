using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class UpdateRaceCommandHandlerTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();

    private Races GivenRace()
    {
        var race = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "Old", Target = 60 };
        _races.GetByIdAsync(_appUserId, race.Id, Arg.Any<CancellationToken>()).Returns(race);
        return race;
    }

    [Fact]
    public async Task Handle_RenamesAndSetsTarget_KeepsTargetWhenOmitted()
    {
        var race = GivenRace();
        var handler = new UpdateRaceCommandHandler(_races);

        var renamed = await handler.Handle(
            new UpdateRaceCommand(_appUserId, race.Id, new CreateRaceRequest(" UTCT ", 70)), CancellationToken.None);
        var kept = await handler.Handle(
            new UpdateRaceCommand(_appUserId, race.Id, new CreateRaceRequest("UTCT 55K", null)), CancellationToken.None);

        Assert.Equal(("UTCT", 70), (renamed.Name, renamed.Target));
        Assert.Equal(("UTCT 55K", 70), (kept.Name, kept.Target));
        await _races.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissingOrNotOwned_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _races.GetByIdAsync(_appUserId, id, Arg.Any<CancellationToken>()).Returns((Races?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateRaceCommandHandler(_races).Handle(
            new UpdateRaceCommand(_appUserId, id, new CreateRaceRequest("A", 1)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithInvalidInput_ThrowsValidationFailedAndSavesNothing()
    {
        var race = GivenRace();

        await Assert.ThrowsAsync<ValidationFailedException>(() => new UpdateRaceCommandHandler(_races).Handle(
            new UpdateRaceCommand(_appUserId, race.Id, new CreateRaceRequest("", -1)), CancellationToken.None));

        Assert.Equal("Old", race.Name);
        await _races.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
