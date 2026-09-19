using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class UpdateItemCommand : IRequest<ItemDto>
{
    public UpdateItemCommand(Guid appUserId, Guid raceId, Guid segmentId, Guid itemId, ItemRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        SegmentId = segmentId;
        ItemId = itemId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid SegmentId { get; }
    public Guid ItemId { get; }
    public ItemRequest Request { get; }
}

internal sealed class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, ItemDto>
{
    private readonly IItemsRepository _itemsRepository;

    public UpdateItemCommandHandler(IItemsRepository itemsRepository)
    {
        _itemsRepository = itemsRepository;
    }

    public async Task<ItemDto> Handle(UpdateItemCommand command, CancellationToken cancellationToken)
    {
        var item = await _itemsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.SegmentId, command.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Items), command.ItemId);

        ItemRequestRules.RequireValid(command.Request);

        item.Name = command.Request.Name!.Trim();
        item.Carbs = command.Request.Carbs ?? 0;

        await _itemsRepository.SaveChangesAsync(cancellationToken);

        return ItemDto.FromEntity(item);
    }
}
