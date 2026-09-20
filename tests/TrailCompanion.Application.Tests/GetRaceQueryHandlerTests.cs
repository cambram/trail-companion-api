using TrailCompanion.Application.Modules.Users.Queries;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class GetRaceQueryHandlerTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly IItemsRepository _items = Substitute.For<IItemsRepository>();
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Races _race;

    public GetRaceQueryHandlerTests()
    {
        _race = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "UTMB", Target = 90 };
        _races.GetByIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns(_race);
        _segments.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([]);
        _items.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([]);
        _nutrition.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([]);
    }

    private GetRaceQueryHandler CreateHandler() => new(_races, _segments, _items, _nutrition);

    private static NutritionItems Nutrition(string name, int? carbs, NutritionType type = NutritionType.Gel, int? size = null) =>
        new() { Id = Guid.NewGuid(), Name = name, Type = type, Carbs = carbs, Size = size };

    [Fact]
    public async Task Handle_GroupsItemsUnderTheirSegmentsInOrderWithLibraryMacros()
    {
        var first = new Segments { Id = Guid.NewGuid(), RaceId = _race.Id, Position = 1, Name = "A", Duration = 90, Distance = 11, Notes = "n" };
        var second = new Segments { Id = Guid.NewGuid(), RaceId = _race.Id, Position = 2, Name = "B" };
        var gel = Nutrition("Gel", 40);
        var chew = Nutrition("Chew", 30);
        var flask = Nutrition("Flask", 50, NutritionType.Liquid, 500);
        _segments.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([first, second]);
        _nutrition.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([gel, chew, flask]);
        _items.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns(
        [
            new Items { Id = Guid.NewGuid(), SegmentId = first.Id, Position = 1, NutritionItemId = gel.Id },
            new Items { Id = Guid.NewGuid(), SegmentId = second.Id, Position = 1, NutritionItemId = flask.Id },
            new Items { Id = Guid.NewGuid(), SegmentId = first.Id, Position = 2, NutritionItemId = chew.Id }
        ]);

        var result = await CreateHandler().Handle(new GetRaceQuery(_appUserId, _race.Id), CancellationToken.None);

        Assert.Equal("UTMB", result.Name);
        Assert.Equal(90, result.Target);
        Assert.Collection(result.Segments,
            s =>
            {
                Assert.Equal(first.Id, s.Id);
                Assert.Equal(11, s.Distance);
                Assert.Equal(["Gel", "Chew"], s.Items.Select(i => i.Name));
                Assert.Equal([40, 30], s.Items.Select(i => i.Carbs));
            },
            s =>
            {
                Assert.Equal(second.Id, s.Id);
                var item = Assert.Single(s.Items);
                Assert.Equal("Flask", item.Name);
                Assert.Equal(NutritionType.Liquid, item.Type);
                Assert.Equal(500, item.Size);
            });
    }

    [Fact]
    public async Task Handle_TheSameLibraryItemTwiceInOneSegment_ReturnsTwoRows()
    {
        var segment = new Segments { Id = Guid.NewGuid(), RaceId = _race.Id, Position = 1, Name = "A" };
        var gel = Nutrition("Gel", 40);
        _segments.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([segment]);
        _nutrition.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([gel]);
        _items.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns(
        [
            new Items { Id = Guid.NewGuid(), SegmentId = segment.Id, Position = 1, NutritionItemId = gel.Id },
            new Items { Id = Guid.NewGuid(), SegmentId = segment.Id, Position = 2, NutritionItemId = gel.Id }
        ]);

        var result = await CreateHandler().Handle(new GetRaceQuery(_appUserId, _race.Id), CancellationToken.None);

        var items = Assert.Single(result.Segments).Items;
        Assert.Equal(2, items.Count);
        Assert.NotEqual(items[0].Id, items[1].Id);
    }

    [Fact]
    public async Task Handle_ItemWithoutAMatchingLibraryEntry_IsSkipped()
    {
        var segment = new Segments { Id = Guid.NewGuid(), RaceId = _race.Id, Position = 1, Name = "A" };
        _segments.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([segment]);
        _items.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns(
        [
            new Items { Id = Guid.NewGuid(), SegmentId = segment.Id, Position = 1, NutritionItemId = Guid.NewGuid() }
        ]);

        var result = await CreateHandler().Handle(new GetRaceQuery(_appUserId, _race.Id), CancellationToken.None);

        Assert.Empty(Assert.Single(result.Segments).Items);
    }

    [Fact]
    public async Task Handle_SegmentWithNoItems_ReturnsEmptyItems()
    {
        var segment = new Segments { Id = Guid.NewGuid(), RaceId = _race.Id, Position = 1, Name = "A" };
        _segments.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([segment]);

        var result = await CreateHandler().Handle(new GetRaceQuery(_appUserId, _race.Id), CancellationToken.None);

        Assert.Empty(Assert.Single(result.Segments).Items);
    }

    [Fact]
    public async Task Handle_RaceWithNoSegments_ReturnsEmptySegments()
    {
        var result = await CreateHandler().Handle(new GetRaceQuery(_appUserId, _race.Id), CancellationToken.None);

        Assert.Empty(result.Segments);
    }

    [Fact]
    public async Task Handle_WhenRaceMissingOrNotOwned_ThrowsNotFoundAndReadsNoChildren()
    {
        var raceId = Guid.NewGuid();
        _races.GetByIdAsync(_appUserId, raceId, Arg.Any<CancellationToken>()).Returns((Races?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetRaceQuery(_appUserId, raceId), CancellationToken.None));

        await _segments.DidNotReceive().GetByRaceIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _items.DidNotReceive().GetByRaceIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _nutrition.DidNotReceive().GetAllAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
