using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteAidStationItemCommand : IRequest
{
    public DeleteAidStationItemCommand(Guid appUserId, Guid raceId, Guid aidStationId, Guid itemId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        AidStationId = aidStationId;
        ItemId = itemId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid AidStationId { get; }
    public Guid ItemId { get; }
}

internal sealed class DeleteAidStationItemCommandHandler : IRequestHandler<DeleteAidStationItemCommand>
{
    private readonly IAidStationItemsRepository _aidStationItemsRepository;

    public DeleteAidStationItemCommandHandler(IAidStationItemsRepository aidStationItemsRepository)
    {
        _aidStationItemsRepository = aidStationItemsRepository;
    }

    public async Task Handle(DeleteAidStationItemCommand command, CancellationToken cancellationToken)
    {
        var item = await _aidStationItemsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.AidStationId, command.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(AidStationItems), command.ItemId);

        _aidStationItemsRepository.Delete(item);
        await _aidStationItemsRepository.SaveChangesAsync(cancellationToken);
    }
}
