using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class StartRunCommand : IRequest<RaceRunStateDto>
{
    public StartRunCommand(Guid appUserId, Guid raceId, StartRunRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public StartRunRequest Request { get; }
}

internal sealed class StartRunCommandHandler : IRequestHandler<StartRunCommand, RaceRunStateDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceRunsRepository _runsRepository;
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly AppTimeZone _zone;
    private readonly TimeProvider _time;

    public StartRunCommandHandler(
        IRacesRepository racesRepository,
        IRaceRunsRepository runsRepository,
        ISegmentsRepository segmentsRepository,
        AppTimeZone zone,
        TimeProvider time)
    {
        _racesRepository = racesRepository;
        _runsRepository = runsRepository;
        _segmentsRepository = segmentsRepository;
        _zone = zone;
        _time = time;
    }

    public async Task<RaceRunStateDto> Handle(StartRunCommand command, CancellationToken cancellationToken)
    {
        var race = await RaceRunAccess.RequireAsync(_racesRepository, command.AppUserId, command.RaceId, cancellationToken);

        if (await _runsRepository.GetByRaceIdAsync(command.AppUserId, command.RaceId, cancellationToken) is not null)
        {
            throw new ConflictException("RunExists", "This race has already been started.");
        }

        var now = _time.GetUtcNow();

        if (race.RaceDate is not { } raceDate)
        {
            throw RunRules.Invalid("RaceDateNotSet", "Set the race date before starting live tracking.");
        }

        var today = _zone.DateOf(now);

        if (today != raceDate)
        {
            throw RunRules.Invalid(
                "NotRaceDay",
                $"Live tracking can only be started on race day ({RunRules.Date(raceDate)}). Today is {RunRules.Date(today)}.");
        }

        var segments = await _segmentsRepository.GetByRaceIdAsync(race.AppUserId, race.Id, cancellationToken);

        if (segments.Count == 0)
        {
            throw RunRules.Invalid("NoSegments", "Add at least one segment to the plan before starting.");
        }

        var startedAt = RunRules.ToUtc(command.Request.StartedAt ?? now);
        RunRules.RequireStart(_zone, race, startedAt, now, null);

        var run = new RaceRuns { RaceId = race.Id, StartedAtUtc = startedAt, StartedBy = command.AppUserId };

        await _runsRepository.AddAsync(run, cancellationToken);
        await _runsRepository.SaveChangesAsync(cancellationToken);

        return RunStateBuilder.Build(_zone, now, race, run, []);
    }
}
