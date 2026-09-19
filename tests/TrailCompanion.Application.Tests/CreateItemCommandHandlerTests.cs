using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class CreateItemCommandHandlerTests
{
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly IItemsRepository _items = Substitute.For<IItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _segmentId = Guid.NewGuid();

    private CreateItemCommandHandler CreateHandler() => new(_segments, _items);

    private CreateItemCommand Command(ItemRequest request) =>
        new(_appUserId, _raceId, _segmentId, request);

    private void GivenOwnedSegment(params Items[] existing)
    {
        _segments.GetByIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>())
            .Returns(new Segments { Id = _segmentId, RaceId = _raceId, Name = "A" });
        _items.GetBySegmentIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>()).Returns(existing);
    }

    [Fact]
    public async Task Handle_OnEmptySegment_CreatesItemAtPositionOne()
    {
        GivenOwnedSegment();

        var result = await CreateHandler().Handle(Command(new ItemRequest("  Maurten Gel 160 ", 40)), CancellationToken.None);

        Assert.Equal(1, result.Position);
        Assert.Equal("Maurten Gel 160", result.Name);
        Assert.Equal(40, result.Carbs);
        Assert.Equal(_segmentId, result.SegmentId);
        await _items.Received(1).AddAsync(Arg.Is<Items>(i => i!.SegmentId == _segmentId), Arg.Any<CancellationToken>());
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingItems_AppendsAfterHighestPosition()
    {
        GivenOwnedSegment(new Items { Position = 1, Name = "A" }, new Items { Position = 6, Name = "B" });

        var result = await CreateHandler().Handle(Command(new ItemRequest("C", null)), CancellationToken.None);

        Assert.Equal(7, result.Position);
        Assert.Equal(0, result.Carbs);
    }

    [Fact]
    public async Task Handle_WhenSegmentMissingOrNotOwned_ThrowsNotFoundAndSavesNothing()
    {
        _segments.GetByIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>()).Returns((Segments?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(Command(new ItemRequest("A", 1)), CancellationToken.None));

        await _items.DidNotReceive().AddAsync(Arg.Any<Items>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData("   ", 10)]
    [InlineData("A", -1)]
    public async Task Handle_WithInvalidInput_ThrowsValidationFailed(string? name, int carbs)
    {
        GivenOwnedSegment();

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(Command(new ItemRequest(name, carbs)), CancellationToken.None));

        await _items.DidNotReceive().AddAsync(Arg.Any<Items>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTooLongName_ThrowsValidationFailed()
    {
        GivenOwnedSegment();

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(Command(new ItemRequest(new string('a', 201), 1)), CancellationToken.None));
    }
}
