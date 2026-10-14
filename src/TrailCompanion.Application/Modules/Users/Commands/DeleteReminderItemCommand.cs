using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteReminderItemCommand : IRequest
{
    public DeleteReminderItemCommand(Guid appUserId, Guid raceId, int reminderNumber, Guid itemId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        ReminderNumber = reminderNumber;
        ItemId = itemId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public int ReminderNumber { get; }
    public Guid ItemId { get; }
}

internal sealed class DeleteReminderItemCommandHandler : IRequestHandler<DeleteReminderItemCommand>
{
    private readonly IReminderItemsRepository _reminderItemsRepository;

    public DeleteReminderItemCommandHandler(IReminderItemsRepository reminderItemsRepository)
    {
        _reminderItemsRepository = reminderItemsRepository;
    }

    public async Task Handle(DeleteReminderItemCommand command, CancellationToken cancellationToken)
    {
        var item = await _reminderItemsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.ReminderNumber, command.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReminderItems), command.ItemId);

        _reminderItemsRepository.Delete(item);
        await _reminderItemsRepository.SaveChangesAsync(cancellationToken);
    }
}
