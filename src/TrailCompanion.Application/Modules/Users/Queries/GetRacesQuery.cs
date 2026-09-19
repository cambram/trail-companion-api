using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Queries;

public sealed class GetRacesQuery : IRequest<IReadOnlyList<RaceDto>>
{
    public GetRacesQuery(Guid appUserId)
    {
        AppUserId = appUserId;
    }

    public Guid AppUserId { get; }
}

internal sealed class GetRacesQueryHandler : IRequestHandler<GetRacesQuery, IReadOnlyList<RaceDto>>
{
    private readonly IRacesRepository _racesRepository;

    public GetRacesQueryHandler(IRacesRepository racesRepository)
    {
        _racesRepository = racesRepository;
    }

    public async Task<IReadOnlyList<RaceDto>> Handle(GetRacesQuery request, CancellationToken cancellationToken)
    {
        var races = await _racesRepository.GetAllAsync(request.AppUserId, cancellationToken);

        return races.Select(RaceDto.FromEntity).ToList();
    }
}
