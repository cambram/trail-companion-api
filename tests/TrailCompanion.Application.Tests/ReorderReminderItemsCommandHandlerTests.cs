using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class ReorderReminderItemsCommandHandlerTests
{
    private readonly IReminderItemsRepository _items = Substitute.For<IReminderItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly ReminderItems _first = new() { Id = Guid.NewGuid(), ReminderNumber = 2, Position = 1 };
    private readonly ReminderItems _second = new() { Id = Guid.NewGuid(), ReminderNumber = 2, Position = 2 };

    public ReorderReminderItemsCommandHandlerTests()
    {
        _items.GetByIdAsync(_appUserId, _raceId, 2, _first.Id, Arg.Any<CancellationToken>()).Returns(_first);
        _items.GetByIdAsync(_appUserId, _raceId, 2, _second.Id, Arg.Any<CancellationToken>()).Returns(_second);
        _items.GetByReminderAsync(_appUserId, _raceId, 2, Arg.Any<CancellationToken>()).Returns([_first, _second]);
    }

    private Task Send(params Guid[] ids) =>
        new ReorderReminderItemsCommandHandler(_items)
            .Handle(new ReorderReminderItemsCommand(_appUserId, _raceId, 2, new ReorderRequest(ids)), CancellationToken.None);

    [Fact]
    public async Task Handle_RewritesPositionsFromOne()
    {
        await Send(_second.Id, _first.Id);

        Assert.Equal(1, _second.Position);
        Assert.Equal(2, _first.Position);
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnIncompleteList_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(_first.Id));
    }

    [Fact]
    public async Task Handle_WithAnUnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Send(_first.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Handle_WithDuplicateIds_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(_first.Id, _first.Id));
    }
}
