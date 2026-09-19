using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteItemCommandHandlerTests
{
    private readonly IItemsRepository _items = Substitute.For<IItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _segmentId = Guid.NewGuid();

    private DeleteItemCommandHandler CreateHandler() => new(_items);

    [Fact]
    public async Task Handle_WhenItemExists_DeletesAndSaves()
    {
        var item = new Items { Id = Guid.NewGuid(), SegmentId = _segmentId, Name = "A" };
        _items.GetByIdAsync(_appUserId, _raceId, _segmentId, item.Id, Arg.Any<CancellationToken>()).Returns(item);

        await CreateHandler().Handle(
            new DeleteItemCommand(_appUserId, _raceId, _segmentId, item.Id), CancellationToken.None);

        _items.Received(1).Delete(item);
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemMissingOrNotOwned_ThrowsNotFoundAndDeletesNothing()
    {
        var itemId = Guid.NewGuid();
        _items.GetByIdAsync(_appUserId, _raceId, _segmentId, itemId, Arg.Any<CancellationToken>()).Returns((Items?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler().Handle(
            new DeleteItemCommand(_appUserId, _raceId, _segmentId, itemId), CancellationToken.None));

        _items.DidNotReceive().Delete(Arg.Any<Items>());
        await _items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
