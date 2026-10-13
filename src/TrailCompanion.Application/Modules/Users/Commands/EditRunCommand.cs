using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class EditRunCommand : IRequest<RaceRunStateDto>
{
    public EditRunCommand(Guid appUserId, Guid raceId, EditRunRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public EditRunRequest Request { get; }
}

internal sealed class EditRunCommandHandler : IRequestHandler<EditRunCommand, RaceRunStateDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceRunsRepository _runsRepository;
    private readonly AppTimeZone _zone;
    private readonly TimeProvider _time;

    public EditRunCommandHandler(
        IRacesRepository racesRepository,
        IRaceRunsRepository runsRepository,
        AppTimeZone zone,
        TimeProvider time)
    {
        _racesRepository = racesRepository;
        _runsRepository = runsRepository;
        _zone = zone;
        _time = time;
    }

    public async Task<RaceRunStateDto> Handle(EditRunCommand command, CancellationToken cancellationToken)
    {
        var race = await RaceRunAccess.RequireAsync(_racesRepository, command.AppUserId, command.RaceId, cancellationToken);
        var run = await _runsRepository.GetByRaceIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException("This race has not been started.");

        if (command.Request.StartedAt is not { } startedAt)
        {
            throw RunRules.Invalid("StartedAtRequired", "Enter the start time.");
        }

        var arrivals = await _runsRepository.GetArrivalsAsync(run.Id, cancellationToken);
        DateTime? firstArrival = arrivals.Count == 0 ? null : arrivals.Min(a => a.ArrivedAtUtc);
        var now = _time.GetUtcNow();
        var utc = RunRules.ToUtc(startedAt);

        RunRules.RequireStart(_zone, race, utc, now, firstArrival);

        run.StartedAtUtc = utc;
        await _runsRepository.SaveChangesAsync(cancellationToken);

        return RunStateBuilder.Build(_zone, now, race, run, arrivals);
    }
}
