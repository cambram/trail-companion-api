using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class RaceRunsRepository : Repository, IRaceRunsRepository
{
    public RaceRunsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<RaceRuns?> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        Context.Set<RaceRuns>()
            .FirstOrDefaultAsync(
                run => run.RaceId == raceId
                       && Context.Set<Races>().Any(r => r.Id == raceId
                           && (r.AppUserId == appUserId
                               || Context.Set<RaceCrewMembers>().Any(c => c.RaceId == raceId && c.AppUserId == appUserId))),
                cancellationToken);

    public async Task<IReadOnlyList<Arrivals>> GetArrivalsAsync(
        Guid raceRunId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Arrivals>()
            .Where(a => a.RaceRunId == raceRunId)
            .OrderBy(a => a.ArrivedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveRunAsync(Guid raceId, CancellationToken cancellationToken = default) =>
        Context.Set<RaceRuns>().AnyAsync(r => r.RaceId == raceId && r.FinishedAtUtc == null, cancellationToken);

    public async Task AddAsync(RaceRuns run, CancellationToken cancellationToken = default) =>
        await AddAsync<RaceRuns>(run, cancellationToken);

    public async Task AddArrivalAsync(Arrivals arrival, CancellationToken cancellationToken = default) =>
        await AddAsync<Arrivals>(arrival, cancellationToken);

    public void Delete(RaceRuns run) => Delete<RaceRuns>(run);

    public void DeleteArrival(Arrivals arrival) => Delete<Arrivals>(arrival);
}
