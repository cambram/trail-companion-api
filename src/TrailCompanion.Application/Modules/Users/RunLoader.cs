using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;

namespace TrailCompanion.Application.Modules.Users;

internal sealed class RunContext
{
    public RunContext(Races race, RaceRuns run, IReadOnlyList<Checkpoint> checkpoints, List<Arrivals> arrivals)
    {
        Race = race;
        Run = run;
        Checkpoints = checkpoints;
        Arrivals = arrivals;
    }

    public Races Race { get; }
    public RaceRuns Run { get; }
    public IReadOnlyList<Checkpoint> Checkpoints { get; }
    public List<Arrivals> Arrivals { get; }

    public Arrivals? ArrivalAt(Checkpoint checkpoint) =>
        Arrivals.FirstOrDefault(a => a.AidStationId == checkpoint.AidStationId);

    public Dictionary<int, DateTime> RecordedExcept(Checkpoint target)
    {
        var indexByKey = Checkpoints.ToDictionary(c => c.Key, c => c.Index);

        return Arrivals
            .Where(a => RunCheckpoints.KeyOf(a) != target.Key && indexByKey.ContainsKey(RunCheckpoints.KeyOf(a)))
            .ToDictionary(a => indexByKey[RunCheckpoints.KeyOf(a)], a => a.ArrivedAtUtc);
    }
}

internal static class RunLoader
{
    public static async Task<RunContext> LoadAsync(
        IRacesRepository racesRepository,
        IRaceRunsRepository runsRepository,
        ISegmentsRepository segmentsRepository,
        IAidStationsRepository aidStationsRepository,
        Guid appUserId,
        Guid raceId,
        CancellationToken cancellationToken)
    {
        var race = await RaceRunAccess.RequireAsync(racesRepository, appUserId, raceId, cancellationToken);
        var run = await runsRepository.GetByRaceIdAsync(appUserId, raceId, cancellationToken)
            ?? throw new NotFoundException("This race has not been started.");

        var segments = await segmentsRepository.GetByRaceIdAsync(race.AppUserId, raceId, cancellationToken);
        var stations = await aidStationsRepository.GetByRaceIdAsync(race.AppUserId, raceId, cancellationToken);
        var arrivals = (await runsRepository.GetArrivalsAsync(run.Id, cancellationToken)).ToList();

        return new RunContext(race, run, RunCheckpoints.Build(segments, stations), arrivals);
    }
}
