using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteRaceCommand : IRequest
{
    public DeleteRaceCommand(Guid appUserId, Guid raceId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
}

internal sealed class DeleteRaceCommandHandler : IRequestHandler<DeleteRaceCommand>
{
    private readonly IRacesRepository _racesRepository;

    public DeleteRaceCommandHandler(IRacesRepository racesRepository)
    {
        _racesRepository = racesRepository;
    }

    public async Task Handle(DeleteRaceCommand command, CancellationToken cancellationToken)
    {
        var race = await _racesRepository.GetByIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), command.RaceId);

        _racesRepository.Delete(race);
        await _racesRepository.SaveChangesAsync(cancellationToken);
    }
}
