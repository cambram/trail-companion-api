using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteNutritionItemCommand : IRequest
{
    public DeleteNutritionItemCommand(Guid appUserId, Guid nutritionItemId)
    {
        AppUserId = appUserId;
        NutritionItemId = nutritionItemId;
    }

    public Guid AppUserId { get; }
    public Guid NutritionItemId { get; }
}

internal sealed class DeleteNutritionItemCommandHandler : IRequestHandler<DeleteNutritionItemCommand>
{
    private readonly INutritionItemsRepository _nutritionItems;

    public DeleteNutritionItemCommandHandler(INutritionItemsRepository nutritionItems)
    {
        _nutritionItems = nutritionItems;
    }

    public async Task Handle(DeleteNutritionItemCommand command, CancellationToken cancellationToken)
    {
        var item = await _nutritionItems.GetByIdAsync(command.AppUserId, command.NutritionItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(NutritionItems), command.NutritionItemId);

        var usages = await _nutritionItems.CountUsagesAsync(
            command.AppUserId, command.NutritionItemId, cancellationToken);

        if (usages > 0)
        {
            throw new ConflictException(
                "NutritionItemInUse",
                $"This item is used {usages} time{(usages == 1 ? string.Empty : "s")} in your race plans. Remove it from those segments, reminders and aid stations first.");
        }

        _nutritionItems.Delete(item);
        await _nutritionItems.SaveChangesAsync(cancellationToken);
    }
}
