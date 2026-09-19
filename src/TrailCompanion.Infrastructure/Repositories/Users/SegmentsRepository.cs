using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class SegmentsRepository : Repository, ISegmentsRepository
{
    public SegmentsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Segments?> GetByIdAsync(
        Guid appUserId,
        Guid raceId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Context.Set<Segments>()
            .FirstOrDefaultAsync(
                s => s.Id == id
                     && s.RaceId == raceId
                     && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId),
                cancellationToken);

    public async Task<IReadOnlyList<Segments>> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Segments>()
            .AsNoTracking()
            .Where(s => s.RaceId == raceId
                        && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId))
            .OrderBy(s => s.Position)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Segments segment, CancellationToken cancellationToken = default) =>
        await AddAsync<Segments>(segment, cancellationToken);

    public void Delete(Segments segment) => Delete<Segments>(segment);
}
