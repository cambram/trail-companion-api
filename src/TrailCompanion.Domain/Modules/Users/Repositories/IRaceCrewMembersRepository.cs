using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IRaceCrewMembersRepository
{
    Task<IReadOnlyList<(RaceCrewMembers Member, AppUser User)>> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default);
    Task<RaceCrewMembers?> GetAsync(Guid raceId, Guid memberId, CancellationToken cancellationToken = default);
    Task<int> CountByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(RaceCrewMembers member, CancellationToken cancellationToken = default);
    void Delete(RaceCrewMembers member);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
