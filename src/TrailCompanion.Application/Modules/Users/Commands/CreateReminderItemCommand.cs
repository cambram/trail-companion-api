using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class CreateReminderItemCommand : IRequest<ReminderItemDto>
{
    public CreateReminderItemCommand(Guid appUserId, Guid raceId, int reminderNumber, ItemRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        ReminderNumber = reminderNumber;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public int ReminderNumber { get; }
    public ItemRequest Request { get; }
}

internal sealed class CreateReminderItemCommandHandler : IRequestHandler<CreateReminderItemCommand, ReminderItemDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly IReminderItemsRepository _reminderItemsRepository;
    private readonly INutritionItemsRepository _nutritionItemsRepository;

    public CreateReminderItemCommandHandler(
        IRacesRepository racesRepository,
        IReminderItemsRepository reminderItemsRepository,
        INutritionItemsRepository nutritionItemsRepository)
    {
        _racesRepository = racesRepository;
        _reminderItemsRepository = reminderItemsRepository;
        _nutritionItemsRepository = nutritionItemsRepository;
    }

    public async Task<ReminderItemDto> Handle(CreateReminderItemCommand command, CancellationToken cancellationToken)
    {
        _ = await _racesRepository.GetByIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), command.RaceId);

        ReminderRequestRules.RequireNumber(command.ReminderNumber);

        if (command.Request.NutritionItemId is not { } nutritionItemId)
        {
            throw new ValidationFailedException(
                "The item request is invalid.",
                [new ValidationFailure("InvalidNutritionItemId", "A nutrition item must be selected.")]);
        }

        var nutritionItem = await _nutritionItemsRepository.GetByIdAsync(
                command.AppUserId, nutritionItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(NutritionItems), nutritionItemId);

        var existing = await _reminderItemsRepository.GetByReminderAsync(
            command.AppUserId, command.RaceId, command.ReminderNumber, cancellationToken);
        AccountLimitRules.RequireRoom(existing.Count, AccountLimitRules.MaxItemsPerReminder, "items on a reminder");

        var item = new ReminderItems
        {
            RaceId = command.RaceId,
            ReminderNumber = command.ReminderNumber,
            NutritionItemId = nutritionItemId,
            Position = existing.Count == 0 ? 1 : existing.Max(i => i.Position) + 1
        };

        await _reminderItemsRepository.AddAsync(item, cancellationToken);
        await _reminderItemsRepository.SaveChangesAsync(cancellationToken);

        return ReminderItemDto.FromEntity(item, nutritionItem);
    }
}
