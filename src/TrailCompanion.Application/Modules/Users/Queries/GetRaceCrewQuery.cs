using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Queries;

public sealed class GetRaceCrewQuery : IRequest<IReadOnlyList<CrewMemberDto>>
{
    public GetRaceCrewQuery(Guid appUserId, Guid raceId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
}

internal sealed class GetRaceCrewQueryHandler : IRequestHandler<GetRaceCrewQuery, IReadOnlyList<CrewMemberDto>>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceCrewMembersRepository _crewRepository;

    public GetRaceCrewQueryHandler(IRacesRepository racesRepository, IRaceCrewMembersRepository crewRepository)
    {
        _racesRepository = racesRepository;
        _crewRepository = crewRepository;
    }

    public async Task<IReadOnlyList<CrewMemberDto>> Handle(GetRaceCrewQuery request, CancellationToken cancellationToken)
    {
        _ = await _racesRepository.GetAccessibleByIdAsync(request.AppUserId, request.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), request.RaceId);

        var members = await _crewRepository.GetByRaceIdAsync(request.AppUserId, request.RaceId, cancellationToken);

        return members.Select(m => CrewMemberDto.FromEntity(m.Member, m.User)).ToList();
    }
}
