using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NpgsqlTypes;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class SegmentEndKmMigrationTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string BeforeEndKm = "20261003050556_AddAidStations";

    private readonly PostgresDatabaseFixture _fixture;

    public SegmentEndKmMigrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Up_TurnsLengthsIntoRunningEndMarksPerRace_AndDownRestoresLengths()
    {
        var (first, second) = await SeedTwoRaces();

        await MigrateTo(BeforeEndKm);
        await using (var context = _fixture.CreateContext())
        {
            await InsertLegacySegment(context, first, 4, null);
            await InsertLegacySegment(context, first, 1, 10);
            await InsertLegacySegment(context, first, 5, 12);
            await InsertLegacySegment(context, first, 2, 0);
            await InsertLegacySegment(context, first, 3, 10);
            await InsertLegacySegment(context, second, 1, 11.3);
            await InsertLegacySegment(context, second, 2, 8.4);
        }

        await MigrateTo(null);
        await using (var context = _fixture.CreateContext())
        {
            Assert.Equal(new double?[] { 10, null, 20, null, 32 }, await EndKms(context, first));
            Assert.Equal(new double?[] { 11.3, 19.7 }, await EndKms(context, second));
        }

        await MigrateTo(BeforeEndKm);
        await using (var context = _fixture.CreateContext())
        {
            Assert.Equal(new double?[] { 10, null, 10, null, 12 }, await LegacyDistances(context, first));
            Assert.Equal(new double?[] { 11.3, 8.4 }, await LegacyDistances(context, second));
        }

        await MigrateTo(null);
    }

    private async Task MigrateTo(string? target)
    {
        await using var context = _fixture.CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(target);
    }

    private async Task<(Guid First, Guid Second)> SeedTwoRaces()
    {
        await using var context = _fixture.CreateContext();
        var appUser = new AppUser { Username = Guid.NewGuid().ToString("N"), PasswordHash = "hash" };
        context.Add(appUser);
        await context.SaveChangesAsync();
        var first = new Races { AppUserId = appUser.Id, Name = "UTCT" };
        var second = new Races { AppUserId = appUser.Id, Name = "Otter" };
        context.AddRange(first, second);
        await context.SaveChangesAsync();
        return (first.Id, second.Id);
    }

    private static Task InsertLegacySegment(ApplicationDbContext context, Guid raceId, int position, double? distance) =>
        context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Segments\" (\"Id\", \"RaceId\", \"Position\", \"Name\", \"Distance\", \"CreatedAtUtc\", \"UpdatedAtUtc\") VALUES ({0}, {1}, {2}, {3}, {4}::double precision, now(), now())",
            Guid.NewGuid(), raceId, position, $"S{position}", new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Double, Value = (object?)distance ?? DBNull.Value });

    private static Task<List<double?>> EndKms(ApplicationDbContext context, Guid raceId) =>
        context.Set<Segments>().Where(s => s.RaceId == raceId).OrderBy(s => s.Position).Select(s => s.EndKm).ToListAsync();

    private static Task<List<double?>> LegacyDistances(ApplicationDbContext context, Guid raceId) =>
        context.Database
            .SqlQueryRaw<double?>("SELECT \"Distance\" AS \"Value\" FROM \"Segments\" WHERE \"RaceId\" = {0} ORDER BY \"Position\"", raceId)
            .ToListAsync();
}
