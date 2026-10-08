using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class RaceCrewMembersRepository : Repository, IRaceCrewMembersRepository
{
    public RaceCrewMembersRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<(RaceCrewMembers Member, AppUser User)>> GetByRaceIdAsync(
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
                from member in Context.Set<RaceCrewMembers>().AsNoTracking()
                join user in Context.AppUsers.AsNoTracking() on member.AppUserId equals user.Id
                where member.RaceId == raceId
                      && Context.Set<Races>().Any(r => r.Id == raceId
                          && (r.AppUserId == appUserId
                              || Context.Set<RaceCrewMembers>().Any(c => c.RaceId == raceId && c.AppUserId == appUserId)))
                orderby member.CreatedAtUtc
                select new { member, user })
            .ToListAsync(cancellationToken);

        return rows.Select(row => (Member: row.member, User: row.user)).ToList();
    }

    public Task<RaceCrewMembers?> GetAsync(
        Guid raceId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        Context.Set<RaceCrewMembers>()
            .FirstOrDefaultAsync(c => c.RaceId == raceId && c.AppUserId == memberId, cancellationToken);

    public Task<int> CountByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default) =>
        Context.Set<RaceCrewMembers>().CountAsync(c => c.RaceId == raceId, cancellationToken);

    public async Task AddAsync(RaceCrewMembers member, CancellationToken cancellationToken = default) =>
        await AddAsync<RaceCrewMembers>(member, cancellationToken);

    public void Delete(RaceCrewMembers member) => Delete<RaceCrewMembers>(member);
}
