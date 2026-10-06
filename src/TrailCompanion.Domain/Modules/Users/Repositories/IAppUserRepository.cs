using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IAppUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AppUser appUser, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
