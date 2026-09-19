using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class UpdateRaceCommand : IRequest<RaceDto>
{
    public UpdateRaceCommand(Guid appUserId, Guid raceId, CreateRaceRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public CreateRaceRequest Request { get; }
}

internal sealed class UpdateRaceCommandHandler : IRequestHandler<UpdateRaceCommand, RaceDto>
{
    private readonly IRacesRepository _racesRepository;

    public UpdateRaceCommandHandler(IRacesRepository racesRepository)
    {
        _racesRepository = racesRepository;
    }

    public async Task<RaceDto> Handle(UpdateRaceCommand command, CancellationToken cancellationToken)
    {
        var race = await _racesRepository.GetByIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), command.RaceId);

        var name = command.Request.Name?.Trim() ?? string.Empty;
        CreateRaceCommandHandler.RequireValid(name, command.Request.Target);

        race.Name = name;
        if (command.Request.Target is { } target)
        {
            race.Target = target;
        }

        await _racesRepository.SaveChangesAsync(cancellationToken);

        return RaceDto.FromEntity(race);
    }
}
