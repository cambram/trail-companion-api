using Microsoft.EntityFrameworkCore;

namespace Template.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class MigrationTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public MigrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AllMigrations_AreApplied()
    {
        await using var context = _fixture.CreateContext();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public void Model_HasNoPendingChangesAgainstTheLastMigration()
    {
        using var context = _fixture.CreateContext();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "The EF model has changed since the last migration. Generate a migration for it.");
    }
}
