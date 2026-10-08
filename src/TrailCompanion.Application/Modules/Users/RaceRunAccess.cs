using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;

namespace TrailCompanion.Application.Modules.Users;

internal static class RaceRunAccess
{
    public static async Task<Races> RequireAsync(
        IRacesRepository racesRepository,
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken) =>
        await racesRepository.GetAccessibleByIdAsync(appUserId, raceId, cancellationToken)
        ?? throw new NotFoundException(nameof(Races), raceId);
}
