using Template.Domain.Modules.Users.Entities;
using Template.Domain.Modules.Users.Repositories;
using Template.Infrastructure.Common.Repositories;
using Template.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Template.Infrastructure.Repositories.Users;

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
