using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class CreateReminderItemCommandHandlerTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly IReminderItemsRepository _items = Substitute.For<IReminderItemsRepository>();
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _nutritionItemId = Guid.NewGuid();

    private CreateReminderItemCommandHandler CreateHandler() => new(_races, _items, _nutrition);

    private Task<ReminderItemDto> Send(int number, Guid? nutritionItemId) =>
        CreateHandler().Handle(
            new CreateReminderItemCommand(_appUserId, _raceId, number, new ItemRequest(nutritionItemId)),
            CancellationToken.None);

    private void GivenOwnedRace(int number = 1, params ReminderItems[] existing)
    {
        _races.GetByIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>())
            .Returns(new Races { Id = _raceId, AppUserId = _appUserId, Name = "UTCT" });
        _items.GetByReminderAsync(_appUserId, _raceId, number, Arg.Any<CancellationToken>()).Returns(existing);
    }

    private void GivenOwnedNutrition() =>
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>())
            .Returns(new NutritionItems { Id = _nutritionItemId, AppUserId = _appUserId, Name = "Stroopwafel", Type = NutritionType.Solid, Carbs = 31 });

    [Fact]
    public async Task Handle_OnEmptyReminder_CreatesAtPositionOneWithLibraryValues()
    {
        GivenOwnedRace();
        GivenOwnedNutrition();

        var result = await Send(1, _nutritionItemId);

        Assert.Equal(1, result.Position);
        Assert.Equal(1, result.ReminderNumber);
        Assert.Equal("Stroopwafel", result.Name);
        Assert.Equal(31, result.Carbs);
        await _items.Received(1).AddAsync(
            Arg.Is<ReminderItems>(i => i!.RaceId == _raceId && i.ReminderNumber == 1),
            Arg.Any<CancellationToken>());
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingItems_AppendsAfterHighestPosition()
    {
        GivenOwnedRace(2, new ReminderItems { Position = 2 }, new ReminderItems { Position = 5 });
        GivenOwnedNutrition();

        var result = await Send(2, _nutritionItemId);

        Assert.Equal(6, result.Position);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task Handle_WithReminderNumberOutOfRange_ThrowsValidationAndStoresNothing(int number)
    {
        GivenOwnedRace(number);
        GivenOwnedNutrition();

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Send(number, _nutritionItemId));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidReminderNumber");
        await _items.DidNotReceive().AddAsync(Arg.Any<ReminderItems>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public async Task Handle_AtTheEdgesOfTheRange_Succeeds(int number)
    {
        GivenOwnedRace(number);
        GivenOwnedNutrition();

        var result = await Send(number, _nutritionItemId);

        Assert.Equal(number, result.ReminderNumber);
    }

    [Fact]
    public async Task Handle_UnknownRaceOrNutrition_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Send(1, _nutritionItemId));

        GivenOwnedRace();
        await Assert.ThrowsAsync<NotFoundException>(() => Send(1, _nutritionItemId));
    }

    [Fact]
    public async Task Handle_WithoutNutritionItem_ThrowsValidation()
    {
        GivenOwnedRace();

        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(1, null));
    }

    [Fact]
    public async Task Handle_AtTheReminderItemLimit_ThrowsAndStoresNothing()
    {
        GivenOwnedRace(1, Enumerable.Range(1, AccountLimitRules.MaxItemsPerReminder)
            .Select(i => new ReminderItems { Position = i })
            .ToArray());
        GivenOwnedNutrition();

        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(1, _nutritionItemId));

        await _items.DidNotReceive().AddAsync(Arg.Any<ReminderItems>(), Arg.Any<CancellationToken>());
    }
}
