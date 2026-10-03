using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class ReorderAidStationItemsCommand : IRequest
{
    public ReorderAidStationItemsCommand(Guid appUserId, Guid raceId, Guid aidStationId, ReorderRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        AidStationId = aidStationId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid AidStationId { get; }
    public ReorderRequest Request { get; }
}

internal sealed class ReorderAidStationItemsCommandHandler : IRequestHandler<ReorderAidStationItemsCommand>
{
    private readonly IAidStationItemsRepository _aidStationItemsRepository;

    public ReorderAidStationItemsCommandHandler(IAidStationItemsRepository aidStationItemsRepository)
    {
        _aidStationItemsRepository = aidStationItemsRepository;
    }

    public async Task Handle(ReorderAidStationItemsCommand command, CancellationToken cancellationToken)
    {
        var ids = ReorderRequestRules.RequireIds(command.Request);
        var items = new List<AidStationItems>();

        foreach (var id in ids)
        {
            items.Add(await _aidStationItemsRepository.GetByIdAsync(
                    command.AppUserId, command.RaceId, command.AidStationId, id, cancellationToken)
                ?? throw new NotFoundException(nameof(AidStationItems), id));
        }

        var existing = await _aidStationItemsRepository.GetByAidStationIdAsync(
            command.AppUserId, command.RaceId, command.AidStationId, cancellationToken);
        ReorderRequestRules.RequireComplete(items.Count, existing.Count);

        for (var i = 0; i < items.Count; i++)
        {
            items[i].Position = i + 1;
        }

        await _aidStationItemsRepository.SaveChangesAsync(cancellationToken);
    }
}
