using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class UpdateItemCommandHandlerTests
{
    private readonly IItemsRepository _items = Substitute.For<IItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _segmentId = Guid.NewGuid();

    private UpdateItemCommandHandler CreateHandler() => new(_items);

    private Items GivenItem()
    {
        var item = new Items { Id = Guid.NewGuid(), SegmentId = _segmentId, Position = 2, Name = "Old", Carbs = 20 };
        _items.GetByIdAsync(_appUserId, _raceId, _segmentId, item.Id, Arg.Any<CancellationToken>()).Returns(item);
        return item;
    }

    [Fact]
    public async Task Handle_ReplacesEditableFieldsAndKeepsPosition()
    {
        var item = GivenItem();

        var result = await CreateHandler().Handle(
            new UpdateItemCommand(_appUserId, _raceId, _segmentId, item.Id, new ItemRequest(" New ", 55)),
            CancellationToken.None);

        Assert.Equal("New", result.Name);
        Assert.Equal(55, result.Carbs);
        Assert.Equal(2, result.Position);
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemMissingOrNotOwned_ThrowsNotFoundAndSavesNothing()
    {
        var itemId = Guid.NewGuid();
        _items.GetByIdAsync(_appUserId, _raceId, _segmentId, itemId, Arg.Any<CancellationToken>()).Returns((Items?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler().Handle(
            new UpdateItemCommand(_appUserId, _raceId, _segmentId, itemId, new ItemRequest("A", 1)),
            CancellationToken.None));

        await _items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidInput_ThrowsValidationFailedAndLeavesItemUntouched()
    {
        var item = GivenItem();

        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new UpdateItemCommand(_appUserId, _raceId, _segmentId, item.Id, new ItemRequest("", -5)),
            CancellationToken.None));

        Assert.Equal("Old", item.Name);
        await _items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
