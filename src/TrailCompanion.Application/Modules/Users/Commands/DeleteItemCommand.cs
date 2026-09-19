using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteItemCommand : IRequest
{
    public DeleteItemCommand(Guid appUserId, Guid raceId, Guid segmentId, Guid itemId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        SegmentId = segmentId;
        ItemId = itemId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid SegmentId { get; }
    public Guid ItemId { get; }
}

internal sealed class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand>
{
    private readonly IItemsRepository _itemsRepository;

    public DeleteItemCommandHandler(IItemsRepository itemsRepository)
    {
        _itemsRepository = itemsRepository;
    }

    public async Task Handle(DeleteItemCommand command, CancellationToken cancellationToken)
    {
        var item = await _itemsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.SegmentId, command.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Items), command.ItemId);

        _itemsRepository.Delete(item);
        await _itemsRepository.SaveChangesAsync(cancellationToken);
    }
}
