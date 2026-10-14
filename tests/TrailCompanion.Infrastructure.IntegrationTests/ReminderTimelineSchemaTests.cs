using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.Repositories.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class ReminderTimelineSchemaTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public ReminderTimelineSchemaTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NewRace_DefaultsToAThirtyMinuteInterval()
    {
        var seed = await RunSeed.Race(_fixture);

        await using var context = _fixture.CreateContext();

        Assert.Equal(30, await context.Set<Races>().Where(r => r.Id == seed.Race.Id).Select(r => r.ReminderIntervalMin).SingleAsync());
    }

    [Theory]
    [InlineData(5)]
    [InlineData(180)]
    public async Task ValidInterval_IsStored(int interval)
    {
        var seed = await RunSeed.Race(_fixture);

        await using var context = _fixture.CreateContext();
        var race = new Races { AppUserId = seed.Owner.Id, Name = "Otter", Pin = TestPins.Next(), ReminderIntervalMin = interval };
        context.Add(race);
        await context.SaveChangesAsync();

        Assert.Equal(interval, await context.Set<Races>().Where(r => r.Id == race.Id).Select(r => r.ReminderIntervalMin).SingleAsync());
    }

    [Theory]
    [InlineData(4)]
    [InlineData(181)]
    public async Task OutOfRangeInterval_ViolatesTheCheckConstraint(int interval)
    {
        var seed = await RunSeed.Race(_fixture);

        await using var context = _fixture.CreateContext();
        context.Add(new Races { AppUserId = seed.Owner.Id, Name = "Otter", Pin = TestPins.Next(), ReminderIntervalMin = interval });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Equal("CK_Races_ReminderIntervalMin", ((PostgresException)exception.InnerException!).ConstraintName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task OutOfRangeReminderNumber_ViolatesTheCheckConstraint(int number)
    {
        var seed = await RunSeed.Race(_fixture);
        var nutrition = await AddNutrition(seed.Owner.Id, NutritionType.Gel);

        await using var context = _fixture.CreateContext();
        context.Add(new ReminderItems { RaceId = seed.Race.Id, ReminderNumber = number, NutritionItemId = nutrition.Id, Position = 1 });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Equal("CK_ReminderItems_ReminderNumber", ((PostgresException)exception.InnerException!).ConstraintName);
    }

    [Fact]
    public async Task DeletingTheRace_CascadesReminderItems_AndDeletingTheLibraryItemIsRestricted()
    {
        var seed = await RunSeed.Race(_fixture);
        var nutrition = await AddNutrition(seed.Owner.Id, NutritionType.Gel);
        await using (var context = _fixture.CreateContext())
        {
            context.Add(new ReminderItems { RaceId = seed.Race.Id, ReminderNumber = 1, NutritionItemId = nutrition.Id, Position = 1 });
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            context.Remove(await context.Set<NutritionItems>().SingleAsync(n => n.Id == nutrition.Id));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = _fixture.CreateContext())
        {
            context.Remove(await context.Set<Races>().SingleAsync(r => r.Id == seed.Race.Id));
            await context.SaveChangesAsync();

            Assert.False(await context.Set<ReminderItems>().AnyAsync(i => i.RaceId == seed.Race.Id));
        }
    }

    [Fact]
    public async Task CountUsagesAsync_CountsReminderItems()
    {
        var seed = await RunSeed.Race(_fixture);
        var nutrition = await AddNutrition(seed.Owner.Id, NutritionType.Gel);
        await using (var context = _fixture.CreateContext())
        {
            context.Add(new ReminderItems { RaceId = seed.Race.Id, ReminderNumber = 1, NutritionItemId = nutrition.Id, Position = 1 });
            context.Add(new ReminderItems { RaceId = seed.Race.Id, ReminderNumber = 2, NutritionItemId = nutrition.Id, Position = 1 });
            await context.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();

        Assert.Equal(2, await new NutritionItemsRepository(read).CountUsagesAsync(seed.Owner.Id, nutrition.Id));
    }

    [Fact]
    public async Task Repository_ReadsOnlyThroughTheOwnedRace_OrderedByNumberThenPosition()
    {
        var seed = await RunSeed.Race(_fixture);
        var stranger = await RunSeed.User(_fixture);
        var nutrition = await AddNutrition(seed.Owner.Id, NutritionType.Gel);
        await using (var context = _fixture.CreateContext())
        {
            context.Add(new ReminderItems { RaceId = seed.Race.Id, ReminderNumber = 2, NutritionItemId = nutrition.Id, Position = 1 });
            context.Add(new ReminderItems { RaceId = seed.Race.Id, ReminderNumber = 1, NutritionItemId = nutrition.Id, Position = 2 });
            context.Add(new ReminderItems { RaceId = seed.Race.Id, ReminderNumber = 1, NutritionItemId = nutrition.Id, Position = 1 });
            await context.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();
        var repository = new ReminderItemsRepository(read);
        var owned = await repository.GetByRaceIdAsync(seed.Owner.Id, seed.Race.Id);
        var foreign = await repository.GetByRaceIdAsync(stranger.Id, seed.Race.Id);

        Assert.Equal([(1, 1), (1, 2), (2, 1)], owned.Select(i => (i.ReminderNumber, i.Position)));
        Assert.Empty(foreign);
    }

    private async Task<NutritionItems> AddNutrition(Guid appUserId, NutritionType type)
    {
        var nutrition = new NutritionItems { AppUserId = appUserId, Name = $"Item {Guid.NewGuid():N}", Type = type, Carbs = 30 };
        await using var context = _fixture.CreateContext();
        context.Add(nutrition);
        await context.SaveChangesAsync();
        return nutrition;
    }
}
