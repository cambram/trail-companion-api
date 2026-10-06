using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.Repositories.Users;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class AidStationRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public AidStationRepositoryTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChangesAsync_WithTwoStationsOnOneSegment_ThrowsConflict()
    {
        var (_, _, segments) = await SeedRace(1);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AidStationsRepository(context);
            await repository.AddAsync(new AidStations { SegmentId = segments[0].Id, Name = "A" });
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AidStationsRepository(context);
            await repository.AddAsync(new AidStations { SegmentId = segments[0].Id, Name = "B" });

            var exception = await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync());

            Assert.Equal("SegmentHasAidStation", exception.Code);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_SwappingTwoStationsInOneSave_Succeeds()
    {
        var (appUserId, raceId, segments) = await SeedRace(2);
        var first = new AidStations { SegmentId = segments[0].Id, Name = "First" };
        var second = new AidStations { SegmentId = segments[1].Id, Name = "Second" };

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AidStationsRepository(context);
            await repository.AddAsync(first);
            await repository.AddAsync(second);
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AidStationsRepository(context);
            var a = await repository.GetByIdAsync(appUserId, raceId, first.Id);
            var b = await repository.GetByIdAsync(appUserId, raceId, second.Id);
            (a!.SegmentId, b!.SegmentId) = (b.SegmentId, a.SegmentId);
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var stations = await new AidStationsRepository(context).GetByRaceIdAsync(appUserId, raceId);

            Assert.Equal(segments[1].Id, stations.Single(s => s.Id == first.Id).SegmentId);
            Assert.Equal(segments[0].Id, stations.Single(s => s.Id == second.Id).SegmentId);
        }
    }

    [Fact]
    public async Task Kit_RoundTripsThroughJsonbWithCertaintyAsText()
    {
        var (appUserId, raceId, segments) = await SeedRace(1);
        var station = new AidStations
        {
            SegmentId = segments[0].Id,
            Name = "Hout Bay",
            Kit = [new KitEntry { Label = "Socks", Certainty = KitCertainty.Planned }, new KitEntry { Label = "Jacket", Certainty = KitCertainty.Maybe }]
        };

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AidStationsRepository(context);
            await repository.AddAsync(station);
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var loaded = await new AidStationsRepository(context).GetByIdAsync(appUserId, raceId, station.Id);

            Assert.Equal(["Socks", "Jacket"], loaded!.Kit.Select(k => k.Label));
            Assert.Equal([KitCertainty.Planned, KitCertainty.Maybe], loaded.Kit.Select(k => k.Certainty));

            var raw = await context.Database
                .SqlQuery<string>($"SELECT \"Kit\"::text AS \"Value\" FROM \"AidStations\" WHERE \"Id\" = {station.Id}")
                .SingleAsync();
            Assert.Contains("\"Maybe\"", raw);
        }
    }

    [Fact]
    public async Task DeletingASegment_CascadesStationAndStationItems_AndLibraryItemIsRestricted()
    {
        var (appUserId, raceId, segments) = await SeedRace(1);
        var nutrition = new NutritionItems { AppUserId = appUserId, Name = $"Sandwich {Guid.NewGuid():N}", Type = NutritionType.Solid, Carbs = 30 };
        var station = new AidStations { SegmentId = segments[0].Id, Name = "Hout Bay" };

        await using (var context = _fixture.CreateContext())
        {
            context.Add(nutrition);
            context.Add(station);
            await context.SaveChangesAsync();
            context.Add(new AidStationItems { AidStationId = station.Id, NutritionItemId = nutrition.Id, Position = 1 });
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            context.Remove(await context.Set<NutritionItems>().SingleAsync(n => n.Id == nutrition.Id));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = _fixture.CreateContext())
        {
            context.Remove(await context.Set<Segments>().SingleAsync(s => s.Id == segments[0].Id));
            await context.SaveChangesAsync();

            Assert.False(await context.Set<AidStations>().AnyAsync(a => a.Id == station.Id));
            Assert.False(await context.Set<AidStationItems>().AnyAsync(i => i.AidStationId == station.Id));
        }
    }

    private async Task<(Guid AppUserId, Guid RaceId, List<Segments> Segments)> SeedRace(int segmentCount)
    {
        var appUser = new AppUser { Username = Guid.NewGuid().ToString("N"), PasswordHash = "hash" };
        await using var context = _fixture.CreateContext();
        context.Add(appUser);
        await context.SaveChangesAsync();
        var race = new Races { AppUserId = appUser.Id, Name = "UTCT" };
        context.Add(race);
        await context.SaveChangesAsync();
        var segments = Enumerable.Range(1, segmentCount)
            .Select(p => new Segments { RaceId = race.Id, Position = p, Name = $"S{p}" })
            .ToList();
        context.AddRange(segments);
        await context.SaveChangesAsync();
        return (appUser.Id, race.Id, segments);
    }
}
