using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class ReorderItemsCommandHandlerTests
{
    private readonly IItemsRepository _items = Substitute.For<IItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _segmentId = Guid.NewGuid();

    private List<Items> GivenItems(int count)
    {
        var list = Enumerable.Range(1, count)
            .Select(p => new Items { Id = Guid.NewGuid(), SegmentId = _segmentId, Position = p, Name = $"I{p}" })
            .ToList();
        foreach (var i in list)
        {
            _items.GetByIdAsync(_appUserId, _raceId, _segmentId, i.Id, Arg.Any<CancellationToken>()).Returns(i);
        }

        _items.GetBySegmentIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>()).Returns(list);
        return list;
    }

    private Task Reorder(params Guid[] ids) => new ReorderItemsCommandHandler(_items).Handle(
        new ReorderItemsCommand(_appUserId, _raceId, _segmentId, new ReorderRequest(ids)), CancellationToken.None);

    [Fact]
    public async Task Handle_AssignsPositionsInListOrder()
    {
        var i = GivenItems(3);

        await Reorder(i[1].Id, i[2].Id, i[0].Id);

        Assert.Equal([3, 1, 2], i.Select(x => x.Position));
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithIncompleteIds_ThrowsValidationFailedAndSavesNothing()
    {
        var i = GivenItems(2);

        await Assert.ThrowsAsync<ValidationFailedException>(() => Reorder(i[0].Id));
        await _items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownId_ThrowsNotFound()
    {
        GivenItems(1);

        await Assert.ThrowsAsync<NotFoundException>(() => Reorder(Guid.NewGuid()));
    }
}
