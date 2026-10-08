using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Application.Modules.Users.Queries;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class GetRacesQueryHandlerTests
{
    private readonly IRacesRepository _repository = Substitute.For<IRacesRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();

    private GetRacesQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReturnsTheCallersRacesAsDtos()
    {
        var first = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "UTMB", Target = 90 };
        var second = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "Western States", Target = 60 };
        _repository.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([first, second]);
        _repository.GetCrewedAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetCrewedAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetRacesQuery(_appUserId), CancellationToken.None);

        Assert.Collection(result,
            r => { Assert.Equal(first.Id, r.Id); Assert.Equal("UTMB", r.Name); Assert.Equal(90, r.Target); },
            r => { Assert.Equal(second.Id, r.Id); Assert.Equal("Western States", r.Name); Assert.Equal(60, r.Target); });
    }

    [Fact]
    public async Task Handle_WithNoRaces_ReturnsAnEmptyList()
    {
        _repository.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetCrewedAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetRacesQuery(_appUserId), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ListsOwnedRacesFirstThenCrewedRacesWithRoles()
    {
        var owned = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "Mine" };
        var crewed = new Races { Id = Guid.NewGuid(), AppUserId = Guid.NewGuid(), Name = "Theirs" };
        _repository.GetAllAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([owned]);
        _repository.GetCrewedAsync(_appUserId, Arg.Any<CancellationToken>()).Returns([crewed]);

        var result = await CreateHandler().Handle(new GetRacesQuery(_appUserId), CancellationToken.None);

        Assert.Collection(result,
            r => { Assert.Equal(owned.Id, r.Id); Assert.Equal(RaceRole.Owner, r.Role); },
            r => { Assert.Equal(crewed.Id, r.Id); Assert.Equal(RaceRole.Crew, r.Role); });
    }
}
