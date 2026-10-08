using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class RaceCrewMigrationTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string BeforeRaceCrew = "20261006182123_AddPasswordChangedAtUtc";
    private const int RaceCount = 50;

    private readonly PostgresDatabaseFixture _fixture;

    public RaceCrewMigrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Up_GivesEveryExistingRaceADistinctSixDigitPin()
    {
        await MigrateTo(BeforeRaceCrew);
        var appUserId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AppUsers" ("Id", "Username", "PasswordHash", "CreatedAtUtc", "UpdatedAtUtc")
                VALUES ({appUserId}, {appUserId.ToString("N")}, 'hash', now(), now())
                """);

            for (var i = 0; i < RaceCount; i++)
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "Races" ("Id", "AppUserId", "Name", "Target", "CreatedAtUtc", "UpdatedAtUtc")
                    VALUES ({Guid.NewGuid()}, {appUserId}, {$"Race {i}"}, 60, now(), now())
                    """);
            }
        }

        await MigrateTo(null);

        await using (var context = _fixture.CreateContext())
        {
            var pins = await context.Set<Races>()
                .Where(r => r.AppUserId == appUserId)
                .Select(r => r.Pin)
                .ToListAsync();

            Assert.Equal(RaceCount, pins.Count);
            Assert.Equal(RaceCount, pins.Distinct().Count());
            Assert.All(pins, pin => Assert.Matches("^[0-9]{6}$", pin));
        }
    }

    private async Task MigrateTo(string? target)
    {
        await using var context = _fixture.CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(target);
    }
}
