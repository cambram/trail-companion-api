using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class UpdateNutritionItemCommand : IRequest<NutritionItemDto>
{
    public UpdateNutritionItemCommand(Guid appUserId, Guid nutritionItemId, NutritionItemRequest request)
    {
        AppUserId = appUserId;
        NutritionItemId = nutritionItemId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid NutritionItemId { get; }
    public NutritionItemRequest Request { get; }
}

internal sealed class UpdateNutritionItemCommandHandler
    : IRequestHandler<UpdateNutritionItemCommand, NutritionItemDto>
{
    private readonly INutritionItemsRepository _nutritionItems;

    public UpdateNutritionItemCommandHandler(INutritionItemsRepository nutritionItems)
    {
        _nutritionItems = nutritionItems;
    }

    public async Task<NutritionItemDto> Handle(
        UpdateNutritionItemCommand command,
        CancellationToken cancellationToken)
    {
        var item = await _nutritionItems.GetByIdAsync(command.AppUserId, command.NutritionItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(NutritionItems), command.NutritionItemId);

        NutritionItemRequestRules.RequireValid(command.Request);

        item.Name = command.Request.Name!.Trim();
        item.Type = command.Request.Type!.Value;
        item.Size = command.Request.Size;
        item.Carbs = command.Request.Carbs;
        item.Calories = command.Request.Calories;
        item.Caffeine = command.Request.Caffeine;
        item.Sodium = command.Request.Sodium;
        item.Potassium = command.Request.Potassium;
        item.Magnesium = command.Request.Magnesium;
        item.Calcium = command.Request.Calcium;

        await _nutritionItems.SaveChangesAsync(cancellationToken);

        return NutritionItemDto.FromEntity(item);
    }
}
