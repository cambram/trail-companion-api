using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteArrivalCommand : IRequest<RaceRunStateDto>
{
    public DeleteArrivalCommand(Guid appUserId, Guid raceId, string checkpoint)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Checkpoint = checkpoint;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public string Checkpoint { get; }
}

internal sealed class DeleteArrivalCommandHandler : IRequestHandler<DeleteArrivalCommand, RaceRunStateDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceRunsRepository _runsRepository;
    private readonly AppTimeZone _zone;
    private readonly TimeProvider _time;

    public DeleteArrivalCommandHandler(
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

    public async Task<RaceRunStateDto> Handle(DeleteArrivalCommand command, CancellationToken cancellationToken)
    {
        var race = await RaceRunAccess.RequireAsync(_racesRepository, command.AppUserId, command.RaceId, cancellationToken);
        var run = await _runsRepository.GetByRaceIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException("This race has not been started.");

        if (!RunCheckpoints.TryParseKey(command.Checkpoint, out var aidStationId))
        {
            throw new NotFoundException("That checkpoint is not on this race.");
        }

        var arrivals = (await _runsRepository.GetArrivalsAsync(run.Id, cancellationToken)).ToList();
        var arrival = arrivals.FirstOrDefault(a => a.AidStationId == aidStationId)
            ?? throw new NotFoundException("That checkpoint has not been recorded.");

        _runsRepository.DeleteArrival(arrival);
        arrivals.Remove(arrival);

        if (aidStationId is null)
        {
            run.FinishedAtUtc = null;
        }

        await _runsRepository.SaveChangesAsync(cancellationToken);

        return RunStateBuilder.Build(_zone, _time.GetUtcNow(), race, run, arrivals);
    }
}
