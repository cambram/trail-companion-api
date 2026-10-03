using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class ReorderAidStationItemsCommandHandlerTests
{
    private readonly IAidStationItemsRepository _items = Substitute.For<IAidStationItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _stationId = Guid.NewGuid();
    private readonly AidStationItems _a = new() { Id = Guid.NewGuid(), Position = 1 };
    private readonly AidStationItems _b = new() { Id = Guid.NewGuid(), Position = 2 };

    public ReorderAidStationItemsCommandHandlerTests()
    {
        _items.GetByIdAsync(_appUserId, _raceId, _stationId, _a.Id, Arg.Any<CancellationToken>()).Returns(_a);
        _items.GetByIdAsync(_appUserId, _raceId, _stationId, _b.Id, Arg.Any<CancellationToken>()).Returns(_b);
        _items.GetByAidStationIdAsync(_appUserId, _raceId, _stationId, Arg.Any<CancellationToken>()).Returns([_a, _b]);
    }

    private Task Send(params Guid[] ids) =>
        new ReorderAidStationItemsCommandHandler(_items).Handle(
            new ReorderAidStationItemsCommand(_appUserId, _raceId, _stationId, new ReorderRequest(ids)),
            CancellationToken.None);

    [Fact]
    public async Task Handle_WithCompleteList_RewritesPositions()
    {
        await Send(_b.Id, _a.Id);

        Assert.Equal(1, _b.Position);
        Assert.Equal(2, _a.Position);
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithIncompleteList_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(_a.Id));
    }

    [Fact]
    public async Task Handle_WithUnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Send(_a.Id, Guid.NewGuid()));
    }
}
