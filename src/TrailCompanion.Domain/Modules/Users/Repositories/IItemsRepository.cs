using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IItemsRepository
{
    Task<Items?> GetByIdAsync(Guid appUserId, Guid raceId, Guid segmentId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Items>> GetBySegmentIdAsync(Guid appUserId, Guid raceId, Guid segmentId, CancellationToken cancellationToken = default);
    Task AddAsync(Items item, CancellationToken cancellationToken = default);
    void Delete(Items item);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
