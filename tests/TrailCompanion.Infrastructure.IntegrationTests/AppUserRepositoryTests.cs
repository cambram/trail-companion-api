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
    public async Task SaveChangesAsync_WithADuplicateUsername_ThrowsConflict()
    {
        var username = Guid.NewGuid().ToString("N");

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AppUserRepository(context);
            await repository.AddAsync(new AppUser { Username = username, PasswordHash = "hash" });
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AppUserRepository(context);
            await repository.AddAsync(new AppUser { Username = username, PasswordHash = "hash" });

            var exception = await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync());

            Assert.Equal("DuplicateUsername", exception.Code);
        }
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsTheStoredUser()
    {
        var appUser = new AppUser { Username = Guid.NewGuid().ToString("N"), PasswordHash = "hash" };

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AppUserRepository(context);
            await repository.AddAsync(appUser);
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var stored = await new AppUserRepository(context).GetByUsernameAsync(appUser.Username);

            Assert.NotNull(stored);
            Assert.Equal(appUser.Id, stored.Id);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAtUtc.Kind);
        }
    }
}
