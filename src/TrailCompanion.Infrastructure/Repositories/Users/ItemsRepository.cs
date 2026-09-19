using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class ItemsRepository : Repository, IItemsRepository
{
    public ItemsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Items?> GetByIdAsync(
        Guid appUserId,
        Guid raceId,
        Guid segmentId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Context.Set<Items>()
            .FirstOrDefaultAsync(
                i => i.Id == id
                     && i.SegmentId == segmentId
                     && Context.Set<Segments>().Any(s => s.Id == segmentId
                         && s.RaceId == raceId
                         && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId)),
                cancellationToken);

    public async Task<IReadOnlyList<Items>> GetBySegmentIdAsync(
        Guid appUserId,
        Guid raceId,
        Guid segmentId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Items>()
            .AsNoTracking()
            .Where(i => i.SegmentId == segmentId
                        && Context.Set<Segments>().Any(s => s.Id == segmentId
                            && s.RaceId == raceId
                            && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId)))
            .OrderBy(i => i.Position)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Items>> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Items>()
            .AsNoTracking()
            .Where(i => Context.Set<Segments>().Any(s => s.Id == i.SegmentId
                && s.RaceId == raceId
                && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId)))
            .OrderBy(i => i.Position)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Items item, CancellationToken cancellationToken = default) =>
        await AddAsync<Items>(item, cancellationToken);

    public void Delete(Items item) => Delete<Items>(item);
}
