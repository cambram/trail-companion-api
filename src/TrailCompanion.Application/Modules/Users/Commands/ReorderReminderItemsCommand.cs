using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class ReorderReminderItemsCommand : IRequest
{
    public ReorderReminderItemsCommand(Guid appUserId, Guid raceId, int reminderNumber, ReorderRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        ReminderNumber = reminderNumber;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public int ReminderNumber { get; }
    public ReorderRequest Request { get; }
}

internal sealed class ReorderReminderItemsCommandHandler : IRequestHandler<ReorderReminderItemsCommand>
{
    private readonly IReminderItemsRepository _reminderItemsRepository;

    public ReorderReminderItemsCommandHandler(IReminderItemsRepository reminderItemsRepository)
    {
        _reminderItemsRepository = reminderItemsRepository;
    }

    public async Task Handle(ReorderReminderItemsCommand command, CancellationToken cancellationToken)
    {
        var ids = ReorderRequestRules.RequireIds(command.Request);
        var items = new List<ReminderItems>();

        foreach (var id in ids)
        {
            items.Add(await _reminderItemsRepository.GetByIdAsync(
                    command.AppUserId, command.RaceId, command.ReminderNumber, id, cancellationToken)
                ?? throw new NotFoundException(nameof(ReminderItems), id));
        }

        var existing = await _reminderItemsRepository.GetByReminderAsync(
            command.AppUserId, command.RaceId, command.ReminderNumber, cancellationToken);
        ReorderRequestRules.RequireComplete(items.Count, existing.Count);

        for (var i = 0; i < items.Count; i++)
        {
            items[i].Position = i + 1;
        }

        await _reminderItemsRepository.SaveChangesAsync(cancellationToken);
    }
}
