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
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Races _race;

    public GetRaceQueryHandlerTests()
    {
        _race = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "UTMB", Target = 90 };
        _races.GetByIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns(_race);
        _segments.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([]);
        _items.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([]);
    }

    private GetRaceQueryHandler CreateHandler() => new(_races, _segments, _items);

    [Fact]
    public async Task Handle_GroupsItemsUnderTheirSegmentsInOrder()
    {
        var first = new Segments { Id = Guid.NewGuid(), RaceId = _race.Id, Position = 1, Name = "A", Duration = 90, Distance = 11, Notes = "n" };
        var second = new Segments { Id = Guid.NewGuid(), RaceId = _race.Id, Position = 2, Name = "B" };
        _segments.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns([first, second]);
        _items.GetByRaceIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns(
        [
            new Items { Id = Guid.NewGuid(), SegmentId = first.Id, Position = 1, Name = "Gel", Carbs = 40 },
            new Items { Id = Guid.NewGuid(), SegmentId = second.Id, Position = 1, Name = "Banana", Carbs = 25 },
            new Items { Id = Guid.NewGuid(), SegmentId = first.Id, Position = 2, Name = "Chew", Carbs = 30 }
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
            },
            s =>
            {
                Assert.Equal(second.Id, s.Id);
                Assert.Equal(["Banana"], s.Items.Select(i => i.Name));
            });
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
    }
}
