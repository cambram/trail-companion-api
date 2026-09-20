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

    public GetRaceQueryHandler(
        IRacesRepository racesRepository,
        ISegmentsRepository segmentsRepository,
        IItemsRepository itemsRepository,
        INutritionItemsRepository nutritionItemsRepository)
    {
        _racesRepository = racesRepository;
        _segmentsRepository = segmentsRepository;
        _itemsRepository = itemsRepository;
        _nutritionItemsRepository = nutritionItemsRepository;
    }

    public async Task<RaceDetailDto> Handle(GetRaceQuery request, CancellationToken cancellationToken)
    {
        var race = await _racesRepository.GetByIdAsync(request.AppUserId, request.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), request.RaceId);

        var segments = await _segmentsRepository.GetByRaceIdAsync(request.AppUserId, request.RaceId, cancellationToken);
        var items = await _itemsRepository.GetByRaceIdAsync(request.AppUserId, request.RaceId, cancellationToken);
        var nutritionItems = await _nutritionItemsRepository.GetAllAsync(request.AppUserId, cancellationToken);

        // ponytail: whole library loaded per race read, join in SQL if a user's library grows past a few hundred items
        var nutritionById = nutritionItems.ToDictionary(n => n.Id);

        var itemsBySegment = items
            .Where(i => nutritionById.ContainsKey(i.NutritionItemId))
            .ToLookup(i => i.SegmentId, i => ItemDto.FromEntity(i, nutritionById[i.NutritionItemId]));

        var segmentDtos = segments
            .Select(s => SegmentDetailDto.FromEntity(s, itemsBySegment[s.Id].ToList()))
            .ToList();

        return RaceDetailDto.FromEntity(race, segmentDtos);
    }
}
