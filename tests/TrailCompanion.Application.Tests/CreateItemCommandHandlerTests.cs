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
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _segmentId = Guid.NewGuid();
    private readonly Guid _nutritionItemId = Guid.NewGuid();

    private CreateItemCommandHandler CreateHandler() => new(_segments, _items, _nutrition);

    private CreateItemCommand Command(Guid? nutritionItemId) =>
        new(_appUserId, _raceId, _segmentId, new ItemRequest(nutritionItemId));

    private void GivenOwnedSegment(params Items[] existing)
    {
        _segments.GetByIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>())
            .Returns(new Segments { Id = _segmentId, RaceId = _raceId, Name = "A" });
        _items.GetBySegmentIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>()).Returns(existing);
    }

    private NutritionItems GivenOwnedNutritionItem()
    {
        var nutritionItem = new NutritionItems
        {
            Id = _nutritionItemId,
            AppUserId = _appUserId,
            Name = "Maurten Gel 160",
            Type = NutritionType.Gel,
            Size = 40,
            Carbs = 40,
            Sodium = 200
        };
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>()).Returns(nutritionItem);
        return nutritionItem;
    }

    [Fact]
    public async Task Handle_OnEmptySegment_CreatesItemAtPositionOneWithLibraryValues()
    {
        GivenOwnedSegment();
        GivenOwnedNutritionItem();

        var result = await CreateHandler().Handle(Command(_nutritionItemId), CancellationToken.None);

        Assert.Equal(1, result.Position);
        Assert.Equal(_segmentId, result.SegmentId);
        Assert.Equal(_nutritionItemId, result.NutritionItemId);
        Assert.Equal("Maurten Gel 160", result.Name);
        Assert.Equal(NutritionType.Gel, result.Type);
        Assert.Equal(40, result.Carbs);
        Assert.Equal(200, result.Sodium);
        Assert.Null(result.Calories);
        await _items.Received(1).AddAsync(
            Arg.Is<Items>(i => i!.SegmentId == _segmentId && i.NutritionItemId == _nutritionItemId),
            Arg.Any<CancellationToken>());
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingItems_AppendsAfterHighestPosition()
    {
        GivenOwnedSegment(new Items { Position = 1 }, new Items { Position = 6 });
        GivenOwnedNutritionItem();

        var result = await CreateHandler().Handle(Command(_nutritionItemId), CancellationToken.None);

        Assert.Equal(7, result.Position);
    }

    [Fact]
    public async Task Handle_WhenSegmentMissingOrNotOwned_ThrowsNotFoundAndSavesNothing()
    {
        _segments.GetByIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>()).Returns((Segments?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(Command(_nutritionItemId), CancellationToken.None));

        await _items.DidNotReceive().AddAsync(Arg.Any<Items>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNutritionItemMissingOrOtherUsers_ThrowsNotFoundAndSavesNothing()
    {
        GivenOwnedSegment();
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>())
            .Returns((NutritionItems?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(Command(_nutritionItemId), CancellationToken.None));

        await _items.DidNotReceive().AddAsync(Arg.Any<Items>(), Arg.Any<CancellationToken>());
        await _items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutANutritionItemId_ThrowsValidationFailedAndSavesNothing()
    {
        GivenOwnedSegment();

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(Command(null), CancellationToken.None));

        await _items.DidNotReceive().AddAsync(Arg.Any<Items>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AtTheItemLimit_ThrowsAndStoresNothing()
    {
        GivenOwnedSegment(Enumerable.Range(1, AccountLimitRules.MaxItemsPerSegment)
            .Select(i => new Items { Position = i })
            .ToArray());
        GivenOwnedNutritionItem();

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(Command(_nutritionItemId), CancellationToken.None));

        await _items.DidNotReceive().AddAsync(Arg.Any<Items>(), Arg.Any<CancellationToken>());
    }
}
