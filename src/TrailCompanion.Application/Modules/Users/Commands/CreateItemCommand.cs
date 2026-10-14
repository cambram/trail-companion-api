using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class CreateItemCommand : IRequest<ItemDto>
{
    public CreateItemCommand(Guid appUserId, Guid raceId, Guid segmentId, ItemRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        SegmentId = segmentId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid SegmentId { get; }
    public ItemRequest Request { get; }
}

internal sealed class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IItemsRepository _itemsRepository;
    private readonly INutritionItemsRepository _nutritionItemsRepository;

    public CreateItemCommandHandler(
        ISegmentsRepository segmentsRepository,
        IItemsRepository itemsRepository,
        INutritionItemsRepository nutritionItemsRepository)
    {
        _segmentsRepository = segmentsRepository;
        _itemsRepository = itemsRepository;
        _nutritionItemsRepository = nutritionItemsRepository;
    }

    public async Task<ItemDto> Handle(CreateItemCommand command, CancellationToken cancellationToken)
    {
        _ = await _segmentsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.SegmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Segments), command.SegmentId);

        if (command.Request.NutritionItemId is not { } nutritionItemId)
        {
            throw new ValidationFailedException(
                "The item request is invalid.",
                [new ValidationFailure("InvalidNutritionItemId", "A nutrition item must be selected.")]);
        }

        var nutritionItem = await _nutritionItemsRepository.GetByIdAsync(
                command.AppUserId, nutritionItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(NutritionItems), nutritionItemId);

        if (nutritionItem.Type != NutritionType.Liquid)
        {
            throw new ValidationFailedException(
                "The item request is invalid.",
                [new ValidationFailure(
                    "LiquidOnly",
                    "Only liquid items can be carried in a segment. Plan solids on the reminder timeline.")]);
        }

        var existing = await _itemsRepository.GetBySegmentIdAsync(
            command.AppUserId, command.RaceId, command.SegmentId, cancellationToken);
        AccountLimitRules.RequireRoom(existing.Count, AccountLimitRules.MaxItemsPerSegment, "items in a segment");

        var item = new Items
        {
            SegmentId = command.SegmentId,
            NutritionItemId = nutritionItemId,
            Position = existing.Count == 0 ? 1 : existing.Max(i => i.Position) + 1
        };

        await _itemsRepository.AddAsync(item, cancellationToken);
        await _itemsRepository.SaveChangesAsync(cancellationToken);

        return ItemDto.FromEntity(item, nutritionItem);
    }
}
