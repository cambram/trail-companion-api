using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteReminderItemCommandHandlerTests
{
    private readonly IReminderItemsRepository _items = Substitute.For<IReminderItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();

    [Fact]
    public async Task Handle_OwnedItem_DeletesAndSaves()
    {
        var item = new ReminderItems { Id = Guid.NewGuid(), ReminderNumber = 3 };
        _items.GetByIdAsync(_appUserId, _raceId, 3, item.Id, Arg.Any<CancellationToken>()).Returns(item);

        await new DeleteReminderItemCommandHandler(_items)
            .Handle(new DeleteReminderItemCommand(_appUserId, _raceId, 3, item.Id), CancellationToken.None);

        _items.Received(1).Delete(item);
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownItem_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => new DeleteReminderItemCommandHandler(_items)
            .Handle(new DeleteReminderItemCommand(_appUserId, _raceId, 3, Guid.NewGuid()), CancellationToken.None));
    }
}
