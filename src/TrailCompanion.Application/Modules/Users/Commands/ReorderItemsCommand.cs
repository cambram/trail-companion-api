using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class ReorderItemsCommand : IRequest
{
    public ReorderItemsCommand(Guid appUserId, Guid raceId, Guid segmentId, ReorderRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        SegmentId = segmentId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid SegmentId { get; }
    public ReorderRequest Request { get; }
}

internal sealed class ReorderItemsCommandHandler : IRequestHandler<ReorderItemsCommand>
{
    private readonly IItemsRepository _itemsRepository;

    public ReorderItemsCommandHandler(IItemsRepository itemsRepository)
    {
        _itemsRepository = itemsRepository;
    }

    public async Task Handle(ReorderItemsCommand command, CancellationToken cancellationToken)
    {
        var ids = ReorderRequestRules.RequireIds(command.Request);
        var items = new List<Items>();

        foreach (var id in ids)
        {
            items.Add(await _itemsRepository.GetByIdAsync(
                    command.AppUserId, command.RaceId, command.SegmentId, id, cancellationToken)
                ?? throw new NotFoundException(nameof(Items), id));
        }

        var existing = await _itemsRepository.GetBySegmentIdAsync(
            command.AppUserId, command.RaceId, command.SegmentId, cancellationToken);
        ReorderRequestRules.RequireComplete(items.Count, existing.Count);

        for (var i = 0; i < items.Count; i++)
        {
            items[i].Position = i + 1;
        }

        await _itemsRepository.SaveChangesAsync(cancellationToken);
    }
}
