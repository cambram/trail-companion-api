using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class RegenerateRacePinCommandHandlerTests
{
    private readonly IRacesRepository _repository = Substitute.For<IRacesRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();

    private RegenerateRacePinCommandHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReplacesThePinAndSaves()
    {
        var race = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "UTMB", Pin = "111111" };
        _repository.GetByIdAsync(_appUserId, race.Id, Arg.Any<CancellationToken>()).Returns(race);

        var result = await CreateHandler().Handle(new RegenerateRacePinCommand(_appUserId, race.Id), CancellationToken.None);

        Assert.Matches("^[0-9]{6}$", result.Pin);
        Assert.Equal(result.Pin, race.Pin);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotTheOwner_ThrowsNotFoundAndSavesNothing()
    {
        var raceId = Guid.NewGuid();
        _repository.GetByIdAsync(_appUserId, raceId, Arg.Any<CancellationToken>()).Returns((Races?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new RegenerateRacePinCommand(_appUserId, raceId), CancellationToken.None));

        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
