using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IReminderItemsRepository
{
    Task<ReminderItems?> GetByIdAsync(Guid appUserId, Guid raceId, int reminderNumber, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderItems>> GetByReminderAsync(Guid appUserId, Guid raceId, int reminderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderItems>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(ReminderItems item, CancellationToken cancellationToken = default);
    void Delete(ReminderItems item);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
