using TrailCompanion.Domain.Modules.Users.Repositories;

namespace TrailCompanion.Application.Modules.Users;

internal static class RunGuard
{
    public static async Task RequireNoActiveRunAsync(
        IRaceRunsRepository runsRepository,
        Guid raceId,
        CancellationToken cancellationToken)
    {
        if (await runsRepository.HasActiveRunAsync(raceId, cancellationToken))
        {
            throw new ConflictException(
                "RunInProgress",
                "This race is being tracked live. Finish or undo the live run before changing its segment order, segments or aid stations.");
        }
    }
}
