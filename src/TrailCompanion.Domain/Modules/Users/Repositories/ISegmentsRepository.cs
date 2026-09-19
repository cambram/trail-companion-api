using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface ISegmentsRepository
{
    Task<Segments?> GetByIdAsync(Guid appUserId, Guid raceId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Segments>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(Segments segment, CancellationToken cancellationToken = default);
    void Delete(Segments segment);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
