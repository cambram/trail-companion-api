using Template.Domain.Modules.Users.Entities;

namespace Template.Infrastructure.Tests;

public class ApplicationDbContextTests
{
    [Fact]
    public void AppUser_Email_HasUniqueIndex()
    {
        using var context = TestDbContext.Create();

        var index = context.Model.FindEntityType(typeof(AppUser))!.GetIndexes()
            .Single(i => i.Properties.Any(p => p.Name == nameof(AppUser.Email)));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public async Task SaveChangesAsync_OnInsert_StampsBothAuditFields()
    {
        using var context = TestDbContext.Create();
        var before = DateTime.UtcNow;

        var user = new AppUser { Email = "insert@example.com", PasswordHash = "hash" };
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

        var user = new AppUser { Email = "update@example.com", PasswordHash = "hash" };
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
