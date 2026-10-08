using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteRunCommand : IRequest<RaceRunStateDto>
{
    public DeleteRunCommand(Guid appUserId, Guid raceId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
}

internal sealed class DeleteRunCommandHandler : IRequestHandler<DeleteRunCommand, RaceRunStateDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceRunsRepository _runsRepository;
    private readonly AppTimeZone _zone;
    private readonly TimeProvider _time;

    public DeleteRunCommandHandler(
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

    public async Task<RaceRunStateDto> Handle(DeleteRunCommand command, CancellationToken cancellationToken)
    {
        var race = await RaceRunAccess.RequireAsync(_racesRepository, command.AppUserId, command.RaceId, cancellationToken);
        var run = await _runsRepository.GetByRaceIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException("This race has not been started.");

        _runsRepository.Delete(run);
        await _runsRepository.SaveChangesAsync(cancellationToken);

        return RunStateBuilder.Build(_zone, _time.GetUtcNow(), race, null, []);
    }
}
