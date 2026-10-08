using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.Repositories.Users;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class RaceCrewRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public RaceCrewRepositoryTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAccessibleByIdAsync_ReturnsTheRaceForOwnerAndCrewOnly()
    {
        var (owner, race) = await SeedRace();
        var crew = await SeedUser();
        var stranger = await SeedUser();
        await AddCrew(race.Id, crew.Id);

        await using var context = _fixture.CreateContext();
        var repository = new RacesRepository(context);

        Assert.NotNull(await repository.GetAccessibleByIdAsync(owner.Id, race.Id));
        Assert.NotNull(await repository.GetAccessibleByIdAsync(crew.Id, race.Id));
        Assert.Null(await repository.GetAccessibleByIdAsync(stranger.Id, race.Id));
        Assert.Null(await repository.GetByIdAsync(crew.Id, race.Id));
    }

    [Fact]
    public async Task GetCrewedAsync_ReturnsOnlyRacesTheCallerCrews()
    {
        var (owner, crewed) = await SeedRace();
        var (_, other) = await SeedRace();
        var crew = await SeedUser();
        await AddCrew(crewed.Id, crew.Id);

        await using var context = _fixture.CreateContext();
        var repository = new RacesRepository(context);

        Assert.Equal([crewed.Id], (await repository.GetCrewedAsync(crew.Id)).Select(r => r.Id));
        Assert.Empty(await repository.GetCrewedAsync(owner.Id));
        Assert.DoesNotContain(other.Id, (await repository.GetCrewedAsync(crew.Id)).Select(r => r.Id));
    }

    [Fact]
    public async Task GetByPinAsync_AndPinExistsAsync_FindTheRace()
    {
        var (_, race) = await SeedRace();

        await using var context = _fixture.CreateContext();
        var repository = new RacesRepository(context);

        Assert.Equal(race.Id, (await repository.GetByPinAsync(race.Pin))!.Id);
        Assert.True(await repository.PinExistsAsync(race.Pin));
        Assert.False(await repository.PinExistsAsync("abcdef"));
    }

    [Fact]
    public async Task SaveChangesAsync_TwoRacesWithOnePin_ThrowsPinTaken()
    {
        var (owner, race) = await SeedRace();

        await using var context = _fixture.CreateContext();
        var repository = new RacesRepository(context);
        await repository.AddAsync(new Races { AppUserId = owner.Id, Name = "Copy", Pin = race.Pin });

        var exception = await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync());

        Assert.Equal("PinTaken", exception.Code);
    }

    [Fact]
    public async Task SaveChangesAsync_SameMemberTwice_ThrowsAlreadyCrew()
    {
        var (_, race) = await SeedRace();
        var crew = await SeedUser();
        await AddCrew(race.Id, crew.Id);

        await using var context = _fixture.CreateContext();
        var repository = new RaceCrewMembersRepository(context);
        await repository.AddAsync(new RaceCrewMembers { RaceId = race.Id, AppUserId = crew.Id });

        var exception = await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync());

        Assert.Equal("AlreadyCrew", exception.Code);
    }

    [Fact]
    public async Task GetByRaceIdAsync_ReturnsMembersWithUsersOnlyToOwnerOrCrew()
    {
        var (owner, race) = await SeedRace();
        var crew = await SeedUser();
        var stranger = await SeedUser();
        await AddCrew(race.Id, crew.Id);

        await using var context = _fixture.CreateContext();
        var repository = new RaceCrewMembersRepository(context);

        var row = Assert.Single(await repository.GetByRaceIdAsync(owner.Id, race.Id));
        Assert.Equal(crew.Id, row.User.Id);
        Assert.Single(await repository.GetByRaceIdAsync(crew.Id, race.Id));
        Assert.Empty(await repository.GetByRaceIdAsync(stranger.Id, race.Id));
        Assert.Equal(1, await repository.CountByRaceIdAsync(race.Id));
    }

    [Fact]
    public async Task DeletingTheRaceOrTheCrewAccount_RemovesTheMembership()
    {
        var (_, first) = await SeedRace();
        var (_, second) = await SeedRace();
        var crew = await SeedUser();
        await AddCrew(first.Id, crew.Id);
        var leaving = await SeedUser();
        await AddCrew(second.Id, leaving.Id);

        await using (var context = _fixture.CreateContext())
        {
            await context.Set<Races>().Where(r => r.Id == first.Id).ExecuteDeleteAsync();
            await new AppUserRepository(context).DeleteAsync(leaving.Id);
        }

        await using (var context = _fixture.CreateContext())
        {
            Assert.False(await context.Set<RaceCrewMembers>().AnyAsync(c => c.RaceId == first.Id));
            Assert.False(await context.Set<RaceCrewMembers>().AnyAsync(c => c.AppUserId == leaving.Id));
        }
    }

    private async Task<AppUser> SeedUser()
    {
        var user = new AppUser { Username = Guid.NewGuid().ToString("N"), PasswordHash = "hash" };
        await using var context = _fixture.CreateContext();
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<(AppUser Owner, Races Race)> SeedRace()
    {
        var owner = await SeedUser();
        var race = new Races { AppUserId = owner.Id, Name = "UTCT", Pin = TestPins.Next() };
        await using var context = _fixture.CreateContext();
        context.Add(race);
        await context.SaveChangesAsync();
        return (owner, race);
    }

    private async Task AddCrew(Guid raceId, Guid appUserId)
    {
        await using var context = _fixture.CreateContext();
        context.Add(new RaceCrewMembers { RaceId = raceId, AppUserId = appUserId });
        await context.SaveChangesAsync();
    }
}
