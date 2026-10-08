using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class JoinRaceCommand : IRequest<RaceDto>
{
    public JoinRaceCommand(Guid appUserId, JoinRaceRequest request)
    {
        AppUserId = appUserId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public JoinRaceRequest Request { get; }
}

internal sealed class JoinRaceCommandHandler : IRequestHandler<JoinRaceCommand, RaceDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceCrewMembersRepository _crewRepository;

    public JoinRaceCommandHandler(IRacesRepository racesRepository, IRaceCrewMembersRepository crewRepository)
    {
        _racesRepository = racesRepository;
        _crewRepository = crewRepository;
    }

    public async Task<RaceDto> Handle(JoinRaceCommand command, CancellationToken cancellationToken)
    {
        var pin = RacePins.Normalize(command.Request.Pin);

        if (!RacePins.IsValid(pin))
        {
            throw new ValidationFailedException(
                "The join request is invalid.",
                [new ValidationFailure("InvalidPin", "Enter the 6-digit race PIN.")]);
        }

        var race = await _racesRepository.GetByPinAsync(pin, cancellationToken)
            ?? throw new NotFoundException("No race found for that PIN.");

        if (race.AppUserId == command.AppUserId)
        {
            throw new ValidationFailedException(
                "The join request is invalid.",
                [new ValidationFailure("OwnRace", "This is your own race.")]);
        }

        if (await _crewRepository.GetAsync(race.Id, command.AppUserId, cancellationToken) is null)
        {
            var count = await _crewRepository.CountByRaceIdAsync(race.Id, cancellationToken);
            AccountLimitRules.RequireRoom(count, AccountLimitRules.MaxCrewPerRace, "crew members on a race");

            await _crewRepository.AddAsync(
                new RaceCrewMembers { RaceId = race.Id, AppUserId = command.AppUserId },
                cancellationToken);

            try
            {
                await _crewRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConflictException exception) when (exception.Code == "AlreadyCrew")
            {
            }
        }

        return RaceDto.FromEntity(race, RaceRole.Crew);
    }
}
