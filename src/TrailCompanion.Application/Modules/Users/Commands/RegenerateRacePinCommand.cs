using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class RegenerateRacePinCommand : IRequest<RacePinDto>
{
    public RegenerateRacePinCommand(Guid appUserId, Guid raceId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
}

internal sealed class RegenerateRacePinCommandHandler : IRequestHandler<RegenerateRacePinCommand, RacePinDto>
{
    private readonly IRacesRepository _racesRepository;

    public RegenerateRacePinCommandHandler(IRacesRepository racesRepository)
    {
        _racesRepository = racesRepository;
    }

    public async Task<RacePinDto> Handle(RegenerateRacePinCommand command, CancellationToken cancellationToken)
    {
        var race = await _racesRepository.GetByIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), command.RaceId);

        race.Pin = await RacePins.GenerateUniqueAsync(_racesRepository, cancellationToken);
        await _racesRepository.SaveChangesAsync(cancellationToken);

        return new RacePinDto(race.Pin);
    }
}
