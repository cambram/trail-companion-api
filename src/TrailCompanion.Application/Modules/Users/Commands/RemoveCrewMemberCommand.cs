using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class RemoveCrewMemberCommand : IRequest
{
    public RemoveCrewMemberCommand(Guid appUserId, Guid raceId, Guid memberId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        MemberId = memberId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid MemberId { get; }
}

internal sealed class RemoveCrewMemberCommandHandler : IRequestHandler<RemoveCrewMemberCommand>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceCrewMembersRepository _crewRepository;

    public RemoveCrewMemberCommandHandler(IRacesRepository racesRepository, IRaceCrewMembersRepository crewRepository)
    {
        _racesRepository = racesRepository;
        _crewRepository = crewRepository;
    }

    public async Task Handle(RemoveCrewMemberCommand command, CancellationToken cancellationToken)
    {
        var race = await _racesRepository.GetAccessibleByIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), command.RaceId);

        if (race.AppUserId != command.AppUserId && command.MemberId != command.AppUserId)
        {
            throw new NotFoundException(nameof(RaceCrewMembers), command.MemberId);
        }

        var member = await _crewRepository.GetAsync(command.RaceId, command.MemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(RaceCrewMembers), command.MemberId);

        _crewRepository.Delete(member);
        await _crewRepository.SaveChangesAsync(cancellationToken);
    }
}
