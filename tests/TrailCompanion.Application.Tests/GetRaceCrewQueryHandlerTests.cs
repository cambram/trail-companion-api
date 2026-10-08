using TrailCompanion.Application.Modules.Users.Queries;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class GetRaceCrewQueryHandlerTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly IRaceCrewMembersRepository _crew = Substitute.For<IRaceCrewMembersRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();

    private GetRaceCrewQueryHandler CreateHandler() => new(_races, _crew);

    [Fact]
    public async Task Handle_ReturnsDisplayNamesFallingBackToUsername()
    {
        _races.GetAccessibleByIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>())
            .Returns(new Races { Id = _raceId, AppUserId = _appUserId });
        var named = new AppUser { Id = Guid.NewGuid(), Username = "alex", FirstName = "Alex", LastName = "M" };
        var bare = new AppUser { Id = Guid.NewGuid(), Username = "jordan" };
        _crew.GetByRaceIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>()).Returns(
        [
            (new RaceCrewMembers { RaceId = _raceId, AppUserId = named.Id }, named),
            (new RaceCrewMembers { RaceId = _raceId, AppUserId = bare.Id }, bare)
        ]);

        var result = await CreateHandler().Handle(new GetRaceCrewQuery(_appUserId, _raceId), CancellationToken.None);

        Assert.Equal(["Alex M", "jordan"], result.Select(m => m.DisplayName));
        Assert.Equal([named.Id, bare.Id], result.Select(m => m.AppUserId));
    }

    [Fact]
    public async Task Handle_WhenNotOwnerOrCrew_ThrowsNotFound()
    {
        _races.GetAccessibleByIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>()).Returns((Races?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetRaceCrewQuery(_appUserId, _raceId), CancellationToken.None));
    }
}
