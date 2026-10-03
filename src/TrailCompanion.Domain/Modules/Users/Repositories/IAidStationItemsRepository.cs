using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IAidStationItemsRepository
{
    Task<AidStationItems?> GetByIdAsync(Guid appUserId, Guid raceId, Guid aidStationId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AidStationItems>> GetByAidStationIdAsync(Guid appUserId, Guid raceId, Guid aidStationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AidStationItems>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(AidStationItems item, CancellationToken cancellationToken = default);
    void Delete(AidStationItems item);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
