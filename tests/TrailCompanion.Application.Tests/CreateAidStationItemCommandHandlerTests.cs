using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class CreateAidStationItemCommandHandlerTests
{
    private readonly IAidStationsRepository _stations = Substitute.For<IAidStationsRepository>();
    private readonly IAidStationItemsRepository _items = Substitute.For<IAidStationItemsRepository>();
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _stationId = Guid.NewGuid();
    private readonly Guid _nutritionItemId = Guid.NewGuid();

    private CreateAidStationItemCommandHandler CreateHandler() => new(_stations, _items, _nutrition);

    private Task<AidStationItemDto> Send(Guid? nutritionItemId) =>
        CreateHandler().Handle(
            new CreateAidStationItemCommand(_appUserId, _raceId, _stationId, new ItemRequest(nutritionItemId)),
            CancellationToken.None);

    private void GivenOwnedStation(params AidStationItems[] existing)
    {
        _stations.GetByIdAsync(_appUserId, _raceId, _stationId, Arg.Any<CancellationToken>())
            .Returns(new AidStations { Id = _stationId, Name = "Hout Bay" });
        _items.GetByAidStationIdAsync(_appUserId, _raceId, _stationId, Arg.Any<CancellationToken>()).Returns(existing);
    }

    private void GivenOwnedNutrition() =>
        _nutrition.GetByIdAsync(_appUserId, _nutritionItemId, Arg.Any<CancellationToken>())
            .Returns(new NutritionItems { Id = _nutritionItemId, AppUserId = _appUserId, Name = "Sandwich", Type = NutritionType.Solid, Carbs = 30 });

    [Fact]
    public async Task Handle_OnEmptyStation_CreatesAtPositionOneWithLibraryValues()
    {
        GivenOwnedStation();
        GivenOwnedNutrition();

        var result = await Send(_nutritionItemId);

        Assert.Equal(1, result.Position);
        Assert.Equal(_stationId, result.AidStationId);
        Assert.Equal("Sandwich", result.Name);
        Assert.Equal(30, result.Carbs);
        await _items.Received(1).AddAsync(Arg.Is<AidStationItems>(i => i.AidStationId == _stationId), Arg.Any<CancellationToken>());
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingItems_AppendsAfterHighestPosition()
    {
        GivenOwnedStation(new AidStationItems { Position = 2 }, new AidStationItems { Position = 5 });
        GivenOwnedNutrition();

        var result = await Send(_nutritionItemId);

        Assert.Equal(6, result.Position);
    }

    [Fact]
    public async Task Handle_UnknownStationOrNutrition_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Send(_nutritionItemId));

        GivenOwnedStation();
        await Assert.ThrowsAsync<NotFoundException>(() => Send(_nutritionItemId));
    }

    [Fact]
    public async Task Handle_WithoutNutritionItem_ThrowsValidation()
    {
        GivenOwnedStation();

        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(null));
    }
}
