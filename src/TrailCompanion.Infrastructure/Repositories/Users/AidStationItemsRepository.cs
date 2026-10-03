using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class AidStationItemsRepository : Repository, IAidStationItemsRepository
{
    public AidStationItemsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<AidStationItems?> GetByIdAsync(
        Guid appUserId,
        Guid raceId,
        Guid aidStationId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Owned(appUserId, raceId)
            .FirstOrDefaultAsync(i => i.Id == id && i.AidStationId == aidStationId, cancellationToken);

    public async Task<IReadOnlyList<AidStationItems>> GetByAidStationIdAsync(
        Guid appUserId,
        Guid raceId,
        Guid aidStationId,
        CancellationToken cancellationToken = default) =>
        await Owned(appUserId, raceId)
            .AsNoTracking()
            .Where(i => i.AidStationId == aidStationId)
            .OrderBy(i => i.Position)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AidStationItems>> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        await Owned(appUserId, raceId)
            .AsNoTracking()
            .OrderBy(i => i.Position)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AidStationItems item, CancellationToken cancellationToken = default) =>
        await AddAsync<AidStationItems>(item, cancellationToken);

    public void Delete(AidStationItems item) => Delete<AidStationItems>(item);

    private IQueryable<AidStationItems> Owned(Guid appUserId, Guid raceId) =>
        Context.Set<AidStationItems>()
            .Where(i => Context.Set<AidStations>().Any(a => a.Id == i.AidStationId
                && Context.Set<Segments>().Any(s => s.Id == a.SegmentId
                    && s.RaceId == raceId
                    && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId))));
}
