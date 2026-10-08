using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class RemoveCrewMemberCommandHandlerTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly IRaceCrewMembersRepository _crew = Substitute.For<IRaceCrewMembersRepository>();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _crewId = Guid.NewGuid();
    private readonly Guid _otherCrewId = Guid.NewGuid();
    private readonly Races _race;

    public RemoveCrewMemberCommandHandlerTests()
    {
        _race = new Races { Id = Guid.NewGuid(), AppUserId = _ownerId, Name = "UTCT" };
        _races.GetAccessibleByIdAsync(_ownerId, _race.Id, Arg.Any<CancellationToken>()).Returns(_race);
        _races.GetAccessibleByIdAsync(_crewId, _race.Id, Arg.Any<CancellationToken>()).Returns(_race);
        _crew.GetAsync(_race.Id, _crewId, Arg.Any<CancellationToken>())
            .Returns(new RaceCrewMembers { RaceId = _race.Id, AppUserId = _crewId });
        _crew.GetAsync(_race.Id, _otherCrewId, Arg.Any<CancellationToken>())
            .Returns(new RaceCrewMembers { RaceId = _race.Id, AppUserId = _otherCrewId });
    }

    private RemoveCrewMemberCommandHandler CreateHandler() => new(_races, _crew);

    private Task Remove(Guid caller, Guid member) =>
        CreateHandler().Handle(new RemoveCrewMemberCommand(caller, _race.Id, member), CancellationToken.None);

    [Fact]
    public async Task Handle_OwnerRemovesAMember()
    {
        await Remove(_ownerId, _crewId);

        _crew.Received(1).Delete(Arg.Is<RaceCrewMembers>(m => m!.AppUserId == _crewId));
        await _crew.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CrewLeavesThemselves()
    {
        await Remove(_crewId, _crewId);

        _crew.Received(1).Delete(Arg.Is<RaceCrewMembers>(m => m!.AppUserId == _crewId));
    }

    [Fact]
    public async Task Handle_CrewRemovingSomeoneElse_ThrowsNotFoundAndDeletesNothing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Remove(_crewId, _otherCrewId));

        _crew.DidNotReceive().Delete(Arg.Any<RaceCrewMembers>());
    }

    [Fact]
    public async Task Handle_StrangerOrUnknownMember_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Remove(Guid.NewGuid(), _crewId));
        await Assert.ThrowsAsync<NotFoundException>(() => Remove(_ownerId, Guid.NewGuid()));
    }
}
