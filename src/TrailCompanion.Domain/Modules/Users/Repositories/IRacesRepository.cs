using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IRacesRepository
{
    Task<Races?> GetByIdAsync(Guid appUserId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Races>> GetAllAsync(Guid appUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Races race, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
