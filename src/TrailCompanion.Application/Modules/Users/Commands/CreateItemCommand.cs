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

    public CreateItemCommandHandler(ISegmentsRepository segmentsRepository, IItemsRepository itemsRepository)
    {
        _segmentsRepository = segmentsRepository;
        _itemsRepository = itemsRepository;
    }

    public async Task<ItemDto> Handle(CreateItemCommand command, CancellationToken cancellationToken)
    {
        _ = await _segmentsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.SegmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Segments), command.SegmentId);

        ItemRequestRules.RequireValid(command.Request);

        var existing = await _itemsRepository.GetBySegmentIdAsync(
            command.AppUserId, command.RaceId, command.SegmentId, cancellationToken);

        var item = new Items
        {
            SegmentId = command.SegmentId,
            Position = existing.Count == 0 ? 1 : existing.Max(i => i.Position) + 1,
            Name = command.Request.Name!.Trim(),
            Carbs = command.Request.Carbs ?? 0
        };

        await _itemsRepository.AddAsync(item, cancellationToken);
        await _itemsRepository.SaveChangesAsync(cancellationToken);

        return ItemDto.FromEntity(item);
    }
}
