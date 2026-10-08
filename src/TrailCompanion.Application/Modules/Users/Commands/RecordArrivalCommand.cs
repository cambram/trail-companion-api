using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class RecordArrivalCommand : IRequest<RaceRunStateDto>
{
    public RecordArrivalCommand(Guid appUserId, Guid raceId, RecordArrivalRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public RecordArrivalRequest Request { get; }
}

internal sealed class RecordArrivalCommandHandler : IRequestHandler<RecordArrivalCommand, RaceRunStateDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceRunsRepository _runsRepository;
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IAidStationsRepository _aidStationsRepository;
    private readonly AppTimeZone _zone;
    private readonly TimeProvider _time;

    public RecordArrivalCommandHandler(
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

    public async Task<RaceRunStateDto> Handle(RecordArrivalCommand command, CancellationToken cancellationToken)
    {
        var context = await RunLoader.LoadAsync(
            _racesRepository,
            _runsRepository,
            _segmentsRepository,
            _aidStationsRepository,
            command.AppUserId,
            command.RaceId,
            cancellationToken);

        var target = RunCheckpoints.Find(context.Checkpoints, command.Request.Checkpoint)
            ?? throw RunRules.Invalid("InvalidCheckpoint", "Choose an aid station or the finish.");

        if (context.ArrivalAt(target) is not null)
        {
            throw new ConflictException("ArrivalExists", $"{target.Name} has already been recorded.");
        }

        var now = _time.GetUtcNow();
        var arrivedAt = RunRules.ToUtc(command.Request.ArrivedAt ?? now);

        RunRules.RequireArrival(
            _zone,
            context.Run.StartedAtUtc,
            context.Checkpoints,
            context.RecordedExcept(target),
            target,
            arrivedAt,
            now);

        var arrival = new Arrivals
        {
            RaceRunId = context.Run.Id,
            AidStationId = target.AidStationId,
            ArrivedAtUtc = arrivedAt,
            RecordedBy = command.AppUserId
        };

        await _runsRepository.AddArrivalAsync(arrival, cancellationToken);

        if (target.AidStationId is null)
        {
            context.Run.FinishedAtUtc = arrivedAt;
        }

        await _runsRepository.SaveChangesAsync(cancellationToken);
        context.Arrivals.Add(arrival);

        return RunStateBuilder.Build(_zone, now, context.Race, context.Run, context.Arrivals);
    }
}
