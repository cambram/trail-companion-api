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
    private readonly INutritionItemsRepository _nutritionItemsRepository;
    private readonly IAidStationsRepository _aidStationsRepository;
    private readonly IAidStationItemsRepository _aidStationItemsRepository;

    public GetRaceQueryHandler(
        IRacesRepository racesRepository,
        ISegmentsRepository segmentsRepository,
        IItemsRepository itemsRepository,
        INutritionItemsRepository nutritionItemsRepository,
        IAidStationsRepository aidStationsRepository,
        IAidStationItemsRepository aidStationItemsRepository)
    {
        _racesRepository = racesRepository;
        _segmentsRepository = segmentsRepository;
        _itemsRepository = itemsRepository;
        _nutritionItemsRepository = nutritionItemsRepository;
        _aidStationsRepository = aidStationsRepository;
        _aidStationItemsRepository = aidStationItemsRepository;
    }

    public async Task<RaceDetailDto> Handle(GetRaceQuery request, CancellationToken cancellationToken)
    {
        var race = await _racesRepository.GetAccessibleByIdAsync(request.AppUserId, request.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), request.RaceId);

        var ownerId = race.AppUserId;
        var role = ownerId == request.AppUserId ? RaceRole.Owner : RaceRole.Crew;

        var segments = await _segmentsRepository.GetByRaceIdAsync(ownerId, request.RaceId, cancellationToken);
        var items = await _itemsRepository.GetByRaceIdAsync(ownerId, request.RaceId, cancellationToken);
        var nutritionItems = await _nutritionItemsRepository.GetAllAsync(ownerId, cancellationToken);

        // ponytail: whole library loaded per race read, join in SQL if a user's library grows past a few hundred items
        var nutritionById = nutritionItems.ToDictionary(n => n.Id);

        var stations = await _aidStationsRepository.GetByRaceIdAsync(ownerId, request.RaceId, cancellationToken);
        var stationItems = await _aidStationItemsRepository.GetByRaceIdAsync(ownerId, request.RaceId, cancellationToken);

        var itemsBySegment = items
            .Where(i => nutritionById.ContainsKey(i.NutritionItemId))
            .ToLookup(i => i.SegmentId, i => ItemDto.FromEntity(i, nutritionById[i.NutritionItemId]));

        var itemsByStation = stationItems
            .Where(i => nutritionById.ContainsKey(i.NutritionItemId))
            .ToLookup(i => i.AidStationId, i => AidStationItemDto.FromEntity(i, nutritionById[i.NutritionItemId]));

        var stationBySegment = stations.ToDictionary(
            a => a.SegmentId,
            a => AidStationDetailDto.FromEntity(a, itemsByStation[a.Id].ToList()));

        var segmentDtos = segments
            .Select(s => SegmentDetailDto.FromEntity(
                s,
                itemsBySegment[s.Id].ToList(),
                stationBySegment.GetValueOrDefault(s.Id)))
            .ToList();

        return RaceDetailDto.FromEntity(race, segmentDtos, role);
    }
}
