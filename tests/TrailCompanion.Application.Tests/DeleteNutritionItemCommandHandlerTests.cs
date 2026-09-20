using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteNutritionItemCommandHandlerTests
{
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _nutritionItemId = Guid.NewGuid();

    private DeleteNutritionItemCommandHandler CreateHandler() => new(_nutrition);

    private DeleteNutritionItemCommand Command() => new(_appUserId, _nutritionItemId);

    private NutritionItems GivenOwnedItem(int usages)
    {
        var item = new NutritionItems { Id = _nutritionItemId, AppUserId = _appUserId, Name = "Gel" };
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>()).Returns(item);
        _nutrition.CountUsagesAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>()).Returns(usages);
        return item;
    }

    [Fact]
    public async Task Handle_WhenUnused_DeletesAndSaves()
    {
        var item = GivenOwnedItem(0);

        await CreateHandler().Handle(Command(), CancellationToken.None);

        _nutrition.Received(1).Delete(item);
        await _nutrition.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, "1 segment.")]
    [InlineData(3, "3 segments.")]
    public async Task Handle_WhenInUse_ThrowsConflictAndSavesNothing(int usages, string expected)
    {
        GivenOwnedItem(usages);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(Command(), CancellationToken.None));

        Assert.Equal("NutritionItemInUse", exception.Code);
        Assert.Contains(expected, exception.Message);
        _nutrition.DidNotReceive().Delete(Arg.Any<NutritionItems>());
        await _nutrition.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissingOrNotOwned_ThrowsNotFoundAndCountsNothing()
    {
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>())
            .Returns((NutritionItems?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(Command(), CancellationToken.None));

        await _nutrition.DidNotReceive().CountUsagesAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _nutrition.DidNotReceive().Delete(Arg.Any<NutritionItems>());
    }
}
