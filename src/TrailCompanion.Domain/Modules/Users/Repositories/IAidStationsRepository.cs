using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IAidStationsRepository
{
    Task<AidStations?> GetByIdAsync(Guid appUserId, Guid raceId, Guid id, CancellationToken cancellationToken = default);
    Task<AidStations?> GetBySegmentIdAsync(Guid appUserId, Guid raceId, Guid segmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AidStations>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(AidStations station, CancellationToken cancellationToken = default);
    void Delete(AidStations station);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
