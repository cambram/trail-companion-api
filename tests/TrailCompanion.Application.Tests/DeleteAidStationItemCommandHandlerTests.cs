using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteAidStationItemCommandHandlerTests
{
    private readonly IAidStationItemsRepository _items = Substitute.For<IAidStationItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _stationId = Guid.NewGuid();

    [Fact]
    public async Task Handle_OwnedItem_DeletesAndSaves()
    {
        var item = new AidStationItems { Id = Guid.NewGuid(), AidStationId = _stationId };
        _items.GetByIdAsync(_appUserId, _raceId, _stationId, item.Id, Arg.Any<CancellationToken>()).Returns(item);

        await new DeleteAidStationItemCommandHandler(_items).Handle(
            new DeleteAidStationItemCommand(_appUserId, _raceId, _stationId, item.Id), CancellationToken.None);

        _items.Received(1).Delete(item);
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownItem_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => new DeleteAidStationItemCommandHandler(_items).Handle(
            new DeleteAidStationItemCommand(_appUserId, _raceId, _stationId, Guid.NewGuid()), CancellationToken.None));
    }
}
