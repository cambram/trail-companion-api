using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class ReminderItemsRepository : Repository, IReminderItemsRepository
{
    public ReminderItemsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<ReminderItems?> GetByIdAsync(
        Guid appUserId,
        Guid raceId,
        int reminderNumber,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Owned(appUserId, raceId)
            .FirstOrDefaultAsync(i => i.Id == id && i.ReminderNumber == reminderNumber, cancellationToken);

    public async Task<IReadOnlyList<ReminderItems>> GetByReminderAsync(
        Guid appUserId,
        Guid raceId,
        int reminderNumber,
        CancellationToken cancellationToken = default) =>
        await Owned(appUserId, raceId)
            .AsNoTracking()
            .Where(i => i.ReminderNumber == reminderNumber)
            .OrderBy(i => i.Position)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReminderItems>> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        await Owned(appUserId, raceId)
            .AsNoTracking()
            .OrderBy(i => i.ReminderNumber)
            .ThenBy(i => i.Position)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ReminderItems item, CancellationToken cancellationToken = default) =>
        await AddAsync<ReminderItems>(item, cancellationToken);

    public void Delete(ReminderItems item) => Delete<ReminderItems>(item);

    private IQueryable<ReminderItems> Owned(Guid appUserId, Guid raceId) =>
        Context.Set<ReminderItems>()
            .Where(i => i.RaceId == raceId
                && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId));
}
