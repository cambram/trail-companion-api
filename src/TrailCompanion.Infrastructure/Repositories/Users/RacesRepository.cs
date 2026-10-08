using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class RacesRepository : Repository, IRacesRepository
{
    public RacesRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Races?> GetByIdAsync(
        Guid appUserId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Context.Set<Races>()
            .FirstOrDefaultAsync(r => r.Id == id && r.AppUserId == appUserId, cancellationToken);

    public async Task<IReadOnlyList<Races>> GetAllAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Races>()
            .AsNoTracking()
            .Where(r => r.AppUserId == appUserId)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Races?> GetAccessibleByIdAsync(
        Guid appUserId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Context.Set<Races>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == id
                     && (r.AppUserId == appUserId
                         || Context.Set<RaceCrewMembers>().Any(c => c.RaceId == id && c.AppUserId == appUserId)),
                cancellationToken);

    public async Task<IReadOnlyList<Races>> GetCrewedAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Races>()
            .AsNoTracking()
            .Where(r => Context.Set<RaceCrewMembers>().Any(c => c.RaceId == r.Id && c.AppUserId == appUserId))
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Races?> GetByPinAsync(string pin, CancellationToken cancellationToken = default) =>
        Context.Set<Races>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Pin == pin, cancellationToken);

    public Task<bool> PinExistsAsync(string pin, CancellationToken cancellationToken = default) =>
        Context.Set<Races>().AnyAsync(r => r.Pin == pin, cancellationToken);

    public async Task AddAsync(Races race, CancellationToken cancellationToken = default) =>
        await AddAsync<Races>(race, cancellationToken);

    public void Delete(Races race) => Delete<Races>(race);
}
