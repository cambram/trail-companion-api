using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteRaceCommandHandlerTests
{
    private readonly IRacesRepository _repository = Substitute.For<IRacesRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();

    private DeleteRaceCommandHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_WhenRaceExists_DeletesAndSaves()
    {
        var race = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "UTMB" };
        _repository.GetByIdAsync(_appUserId, race.Id, Arg.Any<CancellationToken>()).Returns(race);

        await CreateHandler().Handle(new DeleteRaceCommand(_appUserId, race.Id), CancellationToken.None);

        _repository.Received(1).Delete(race);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRaceMissingOrNotOwned_ThrowsNotFoundAndDeletesNothing()
    {
        var raceId = Guid.NewGuid();
        _repository.GetByIdAsync(_appUserId, raceId, Arg.Any<CancellationToken>()).Returns((Races?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteRaceCommand(_appUserId, raceId), CancellationToken.None));

        _repository.DidNotReceive().Delete(Arg.Any<Races>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
