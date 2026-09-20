using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class UpdateNutritionItemCommandHandlerTests
{
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _nutritionItemId = Guid.NewGuid();

    private UpdateNutritionItemCommandHandler CreateHandler() => new(_nutrition);

    private NutritionItems GivenOwnedItem() =>
        GivenOwnedItem(new NutritionItems
        {
            Id = _nutritionItemId,
            AppUserId = _appUserId,
            Name = "Old",
            Type = NutritionType.Gel,
            Size = 40,
            Carbs = 25,
            Sodium = 100
        });

    private NutritionItems GivenOwnedItem(NutritionItems item)
    {
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>()).Returns(item);
        return item;
    }

    [Fact]
    public async Task Handle_ReplacesEveryFieldAndClearsOmittedMacros()
    {
        var item = GivenOwnedItem();
        var request = new NutritionItemRequest(
            "  Tailwind flask ", NutritionType.Liquid, 500, 50, 200, null, 400, 100, 20, 30);

        var result = await CreateHandler().Handle(
            new UpdateNutritionItemCommand(_appUserId, _nutritionItemId, request), CancellationToken.None);

        Assert.Equal("Tailwind flask", result.Name);
        Assert.Equal(NutritionType.Liquid, result.Type);
        Assert.Equal(500, result.Size);
        Assert.Equal(50, result.Carbs);
        Assert.Equal(200, result.Calories);
        Assert.Null(result.Caffeine);
        Assert.Equal(400, result.Sodium);
        Assert.Equal(100, result.Potassium);
        Assert.Equal(20, result.Magnesium);
        Assert.Equal(30, result.Calcium);
        Assert.Equal("Tailwind flask", item.Name);
        Assert.Null(item.Caffeine);
        await _nutrition.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissingOrNotOwned_ThrowsNotFoundAndSavesNothing()
    {
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>())
            .Returns((NutritionItems?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new UpdateNutritionItemCommand(
                    _appUserId,
                    _nutritionItemId,
                    new NutritionItemRequest("A", NutritionType.Gel, null, null, null, null, null, null, null, null)),
                CancellationToken.None));

        await _nutrition.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnInvalidRequest_ThrowsAndLeavesTheItemUntouched()
    {
        var item = GivenOwnedItem();

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(
                new UpdateNutritionItemCommand(
                    _appUserId,
                    _nutritionItemId,
                    new NutritionItemRequest(" ", NutritionType.Gel, null, null, null, null, null, null, null, null)),
                CancellationToken.None));

        Assert.Equal("Old", item.Name);
        await _nutrition.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
