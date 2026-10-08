using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IRaceRunsRepository
{
    Task<RaceRuns?> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Arrivals>> GetArrivalsAsync(Guid raceRunId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveRunAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(RaceRuns run, CancellationToken cancellationToken = default);
    Task AddArrivalAsync(Arrivals arrival, CancellationToken cancellationToken = default);
    void Delete(RaceRuns run);
    void DeleteArrival(Arrivals arrival);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
