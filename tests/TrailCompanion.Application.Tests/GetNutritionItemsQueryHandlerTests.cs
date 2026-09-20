using TrailCompanion.Application.Modules.Users.Queries;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class GetNutritionItemsQueryHandlerTests
{
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();

    private GetNutritionItemsQueryHandler CreateHandler() => new(_nutrition);

    [Fact]
    public async Task Handle_MapsEveryFieldAndKeepsRepositoryOrder()
    {
        var flask = new NutritionItems
        {
            Id = Guid.NewGuid(),
            AppUserId = _appUserId,
            Name = "Alpha flask",
            Type = NutritionType.Liquid,
            Size = 500,
            Carbs = 50,
            Calories = 200,
            Caffeine = 0,
            Sodium = 400,
            Potassium = 100,
            Magnesium = 20,
            Calcium = 30
        };
        var gel = new NutritionItems
        {
            Id = Guid.NewGuid(),
            AppUserId = _appUserId,
            Name = "Zip gel",
            Type = NutritionType.Gel
        };
        _nutrition.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([flask, gel]);

        var result = await CreateHandler().Handle(new GetNutritionItemsQuery(_appUserId), CancellationToken.None);

        Assert.Equal(["Alpha flask", "Zip gel"], result.Select(n => n.Name));
        var first = result[0];
        Assert.Equal(flask.Id, first.Id);
        Assert.Equal(NutritionType.Liquid, first.Type);
        Assert.Equal(500, first.Size);
        Assert.Equal(50, first.Carbs);
        Assert.Equal(200, first.Calories);
        Assert.Equal(0, first.Caffeine);
        Assert.Equal(400, first.Sodium);
        Assert.Equal(100, first.Potassium);
        Assert.Equal(20, first.Magnesium);
        Assert.Equal(30, first.Calcium);
        Assert.Null(result[1].Carbs);
    }

    [Fact]
    public async Task Handle_WithAnEmptyLibrary_ReturnsEmpty()
    {
        _nutrition.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetNutritionItemsQuery(_appUserId), CancellationToken.None);

        Assert.Empty(result);
    }
}
