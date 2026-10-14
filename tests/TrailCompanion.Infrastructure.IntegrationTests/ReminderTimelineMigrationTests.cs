using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class ReminderTimelineMigrationTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string BeforeReminders = "20261013183806_AddSegmentAscent";

    private readonly PostgresDatabaseFixture _fixture;

    public ReminderTimelineMigrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Up_DeletesNonLiquidSegmentItems_KeepsLiquidOnes_AndGivesExistingRacesAThirtyMinuteInterval()
    {
        var seed = await RunSeed.Race(_fixture);
        var gel = new NutritionItems { AppUserId = seed.Owner.Id, Name = $"Gel {Guid.NewGuid():N}", Type = NutritionType.Gel, Carbs = 40 };
        var mix = new NutritionItems { AppUserId = seed.Owner.Id, Name = $"Mix {Guid.NewGuid():N}", Type = NutritionType.Liquid, Carbs = 20, Size = 500 };
        await using (var context = _fixture.CreateContext())
        {
            context.AddRange(gel, mix);
            await context.SaveChangesAsync();
            context.Add(new Items { SegmentId = seed.Segment.Id, NutritionItemId = gel.Id, Position = 1 });
            context.Add(new Items { SegmentId = seed.Segment.Id, NutritionItemId = mix.Id, Position = 2 });
            await context.SaveChangesAsync();
        }

        await MigrateTo(BeforeReminders);
        await MigrateTo(null);

        await using var read = _fixture.CreateContext();
        var remaining = await read.Set<Items>().Where(i => i.SegmentId == seed.Segment.Id).Select(i => i.NutritionItemId).ToListAsync();
        var interval = await read.Set<Races>().Where(r => r.Id == seed.Race.Id).Select(r => r.ReminderIntervalMin).SingleAsync();
        Assert.Equal([mix.Id], remaining);
        Assert.Equal(30, interval);
    }

    private async Task MigrateTo(string? target)
    {
        await using var context = _fixture.CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(target);
    }
}
