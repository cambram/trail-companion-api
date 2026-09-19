using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Queries;

public sealed class GetRaceQuery : IRequest<RaceDetailDto>
{
    public GetRaceQuery(Guid appUserId, Guid raceId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
}

internal sealed class GetRaceQueryHandler : IRequestHandler<GetRaceQuery, RaceDetailDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IItemsRepository _itemsRepository;

    public GetRaceQueryHandler(
        IRacesRepository racesRepository,
        ISegmentsRepository segmentsRepository,
        IItemsRepository itemsRepository)
    {
        _racesRepository = racesRepository;
        _segmentsRepository = segmentsRepository;
        _itemsRepository = itemsRepository;
    }

    public async Task<RaceDetailDto> Handle(GetRaceQuery request, CancellationToken cancellationToken)
    {
        var race = await _racesRepository.GetByIdAsync(request.AppUserId, request.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), request.RaceId);

        var segments = await _segmentsRepository.GetByRaceIdAsync(request.AppUserId, request.RaceId, cancellationToken);
        var items = await _itemsRepository.GetByRaceIdAsync(request.AppUserId, request.RaceId, cancellationToken);
        var itemsBySegment = items.ToLookup(i => i.SegmentId, ItemDto.FromEntity);

        var segmentDtos = segments
            .Select(s => SegmentDetailDto.FromEntity(s, itemsBySegment[s.Id].ToList()))
            .ToList();

        return RaceDetailDto.FromEntity(race, segmentDtos);
    }
}
