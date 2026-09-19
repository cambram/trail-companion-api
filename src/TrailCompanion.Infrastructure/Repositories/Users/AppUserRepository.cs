using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class AppUserRepository : Repository, IAppUserRepository
{
    public AppUserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<AppUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        Context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<AppUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Context.AppUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(AppUser appUser, CancellationToken cancellationToken = default) =>
        await AddAsync<AppUser>(appUser, cancellationToken);
}
