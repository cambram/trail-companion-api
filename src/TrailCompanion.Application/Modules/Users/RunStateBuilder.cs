using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users;

internal static class RunStateBuilder
{
    public static RaceRunStateDto Build(
        AppTimeZone zone,
        DateTimeOffset now,
        Races race,
        RaceRuns? run,
        IReadOnlyList<Arrivals> arrivals)
    {
        var today = zone.DateOf(now);
        var runDto = run is null
            ? null
            : new RaceRunDto(
                run.Id,
                run.StartedAtUtc,
                run.FinishedAtUtc,
                arrivals.OrderBy(a => a.ArrivedAtUtc).Select(ArrivalDto.FromEntity).ToList());

        return new RaceRunStateDto(zone.Id, today, run is null && race.RaceDate == today, runDto);
    }
}
