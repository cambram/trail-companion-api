using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Infrastructure.Tests;

public class ApplicationDbContextTests
{
    [Fact]
    public void AppUser_Username_HasUniqueIndex()
    {
        using var context = TestDbContext.Create();

        var index = context.Model.FindEntityType(typeof(AppUser))!.GetIndexes()
            .Single(i => i.Properties.Any(p => p.Name == nameof(AppUser.Username)));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public async Task SaveChangesAsync_OnInsert_StampsBothAuditFields()
    {
        using var context = TestDbContext.Create();
        var before = DateTime.UtcNow;

        var user = new AppUser { Username = "insert", PasswordHash = "hash" };
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();

        Assert.True(user.CreatedAtUtc >= before);
        Assert.Equal(user.CreatedAtUtc, user.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task SaveChangesAsync_OnUpdate_MovesUpdatedAtButPreservesCreatedAt()
    {
        using var context = TestDbContext.Create();

        var user = new AppUser { Username = "update", PasswordHash = "hash" };
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();

        var originalCreated = user.CreatedAtUtc;
        var originalUpdated = user.UpdatedAtUtc;

        await Task.Delay(50);

        user.PasswordHash = "rehashed";
        await context.SaveChangesAsync();

        Assert.True(user.UpdatedAtUtc > originalUpdated);
        Assert.Equal(originalCreated, user.CreatedAtUtc);
    }
}
