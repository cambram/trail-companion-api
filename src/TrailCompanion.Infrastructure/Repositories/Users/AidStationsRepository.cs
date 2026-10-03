using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class AidStationsRepository : Repository, IAidStationsRepository
{
    public AidStationsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<AidStations?> GetByIdAsync(
        Guid appUserId,
        Guid raceId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Owned(appUserId, raceId).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<AidStations?> GetBySegmentIdAsync(
        Guid appUserId,
        Guid raceId,
        Guid segmentId,
        CancellationToken cancellationToken = default) =>
        Owned(appUserId, raceId).FirstOrDefaultAsync(a => a.SegmentId == segmentId, cancellationToken);

    public async Task<IReadOnlyList<AidStations>> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        await Owned(appUserId, raceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AidStations station, CancellationToken cancellationToken = default) =>
        await AddAsync<AidStations>(station, cancellationToken);

    public void Delete(AidStations station) => Delete<AidStations>(station);

    private IQueryable<AidStations> Owned(Guid appUserId, Guid raceId) =>
        Context.Set<AidStations>()
            .Where(a => Context.Set<Segments>().Any(s => s.Id == a.SegmentId
                && s.RaceId == raceId
                && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId)));
}
