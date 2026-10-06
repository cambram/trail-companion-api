using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class RenameEmailToUsernameMigrationTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string BeforeUsername = "20261006155139_SegmentEndKm";

    private readonly PostgresDatabaseFixture _fixture;

    public RenameEmailToUsernameMigrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Up_KeepsTheUserAndUsesTheEmailLocalPart()
    {
        var id = Guid.NewGuid();

        await MigrateTo(BeforeUsername);
        await using (var context = _fixture.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"AppUsers\" (\"Id\", \"Email\", \"PasswordHash\", \"CreatedAtUtc\", \"UpdatedAtUtc\") VALUES ({0}, {1}, {2}, now(), now())",
                id, "me.runner@example.com", "stored-hash");
        }

        await MigrateTo(null);
        await using (var context = _fixture.CreateContext())
        {
            var migrated = await context.AppUsers.AsNoTracking().SingleAsync(u => u.Id == id);

            Assert.Equal("me.runner", migrated.Username);
            Assert.Equal("stored-hash", migrated.PasswordHash);
        }
    }

    private async Task MigrateTo(string? target)
    {
        await using var context = _fixture.CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(target);
    }
}
