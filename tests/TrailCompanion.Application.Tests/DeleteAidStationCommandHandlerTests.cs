using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteAidStationCommandHandlerTests
{
    private readonly IAidStationsRepository _stations = Substitute.For<IAidStationsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();

    [Fact]
    public async Task Handle_OwnedStation_DeletesAndSaves()
    {
        var station = new AidStations { Id = Guid.NewGuid(), Name = "A" };
        _stations.GetByIdAsync(_appUserId, _raceId, station.Id, Arg.Any<CancellationToken>()).Returns(station);

        await new DeleteAidStationCommandHandler(_stations)
            .Handle(new DeleteAidStationCommand(_appUserId, _raceId, station.Id), CancellationToken.None);

        _stations.Received(1).Delete(station);
        await _stations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownStation_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => new DeleteAidStationCommandHandler(_stations)
            .Handle(new DeleteAidStationCommand(_appUserId, _raceId, Guid.NewGuid()), CancellationToken.None));
    }
}
