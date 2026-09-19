using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.Repositories.Users;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class AppUserRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public AppUserRepositoryTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChangesAsync_WithADuplicateEmail_ThrowsConflict()
    {
        const string email = "duplicate@example.com";

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AppUserRepository(context);
            await repository.AddAsync(new AppUser { Email = email, PasswordHash = "hash" });
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AppUserRepository(context);
            await repository.AddAsync(new AppUser { Email = email, PasswordHash = "hash" });

            var exception = await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync());

            Assert.Equal("DuplicateEmail", exception.Code);
        }
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsTheStoredUser()
    {
        var appUser = new AppUser { Email = $"{Guid.NewGuid():N}@example.com", PasswordHash = "hash" };

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AppUserRepository(context);
            await repository.AddAsync(appUser);
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var stored = await new AppUserRepository(context).GetByEmailAsync(appUser.Email);

            Assert.NotNull(stored);
            Assert.Equal(appUser.Id, stored.Id);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAtUtc.Kind);
        }
    }
}
