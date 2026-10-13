using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class EditArrivalCommand : IRequest<RaceRunStateDto>
{
    public EditArrivalCommand(Guid appUserId, Guid raceId, string checkpoint, EditArrivalRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Checkpoint = checkpoint;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public string Checkpoint { get; }
    public EditArrivalRequest Request { get; }
}

internal sealed class EditArrivalCommandHandler : IRequestHandler<EditArrivalCommand, RaceRunStateDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceRunsRepository _runsRepository;
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IAidStationsRepository _aidStationsRepository;
    private readonly AppTimeZone _zone;
    private readonly TimeProvider _time;

    public EditArrivalCommandHandler(
        IRacesRepository racesRepository,
        IRaceRunsRepository runsRepository,
        ISegmentsRepository segmentsRepository,
        IAidStationsRepository aidStationsRepository,
        AppTimeZone zone,
        TimeProvider time)
    {
        _racesRepository = racesRepository;
        _runsRepository = runsRepository;
        _segmentsRepository = segmentsRepository;
        _aidStationsRepository = aidStationsRepository;
        _zone = zone;
        _time = time;
    }

    public async Task<RaceRunStateDto> Handle(EditArrivalCommand command, CancellationToken cancellationToken)
    {
        var context = await RunLoader.LoadAsync(
            _racesRepository,
            _runsRepository,
            _segmentsRepository,
            _aidStationsRepository,
            command.AppUserId,
            command.RaceId,
            cancellationToken);

        var target = RunCheckpoints.Find(context.Checkpoints, command.Checkpoint)
            ?? throw new NotFoundException("That checkpoint is not on this race.");
        var arrival = context.ArrivalAt(target)
            ?? throw new NotFoundException($"{target.Name} has not been recorded.");

        if (command.Request.ArrivedAt is not { } given)
        {
            throw RunRules.Invalid("ArrivedAtRequired", "Enter the arrival time.");
        }

        var now = _time.GetUtcNow();
        var arrivedAt = RunRules.ToUtc(given);

        RunRules.RequireArrival(
            _zone,
            context.Run.StartedAtUtc,
            context.Checkpoints,
            context.RecordedExcept(target),
            target,
            arrivedAt,
            now);

        arrival.ArrivedAtUtc = arrivedAt;

        if (target.AidStationId is null)
        {
            context.Run.FinishedAtUtc = arrivedAt;
        }

        await _runsRepository.SaveChangesAsync(cancellationToken);

        return RunStateBuilder.Build(_zone, now, context.Race, context.Run, context.Arrivals);
    }
}
