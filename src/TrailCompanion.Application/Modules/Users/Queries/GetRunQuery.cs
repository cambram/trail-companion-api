using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Queries;

public sealed class GetRunQuery : IRequest<RaceRunStateDto>
{
    public GetRunQuery(Guid appUserId, Guid raceId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
}

internal sealed class GetRunQueryHandler : IRequestHandler<GetRunQuery, RaceRunStateDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IRaceRunsRepository _runsRepository;
    private readonly AppTimeZone _zone;
    private readonly TimeProvider _time;

    public GetRunQueryHandler(
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

    public async Task<RaceRunStateDto> Handle(GetRunQuery request, CancellationToken cancellationToken)
    {
        var race = await RaceRunAccess.RequireAsync(_racesRepository, request.AppUserId, request.RaceId, cancellationToken);
        var run = await _runsRepository.GetByRaceIdAsync(request.AppUserId, request.RaceId, cancellationToken);
        IReadOnlyList<Arrivals> arrivals = run is null
            ? []
            : await _runsRepository.GetArrivalsAsync(run.Id, cancellationToken);

        return RunStateBuilder.Build(_zone, _time.GetUtcNow(), race, run, arrivals);
    }
}
