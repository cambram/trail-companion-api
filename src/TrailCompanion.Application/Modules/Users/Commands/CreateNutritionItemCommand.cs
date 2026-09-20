using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class CreateNutritionItemCommand : IRequest<NutritionItemDto>
{
    public CreateNutritionItemCommand(Guid appUserId, NutritionItemRequest request)
    {
        AppUserId = appUserId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public NutritionItemRequest Request { get; }
}

internal sealed class CreateNutritionItemCommandHandler
    : IRequestHandler<CreateNutritionItemCommand, NutritionItemDto>
{
    private readonly INutritionItemsRepository _nutritionItems;

    public CreateNutritionItemCommandHandler(INutritionItemsRepository nutritionItems)
    {
        _nutritionItems = nutritionItems;
    }

    public async Task<NutritionItemDto> Handle(
        CreateNutritionItemCommand command,
        CancellationToken cancellationToken)
    {
        NutritionItemRequestRules.RequireValid(command.Request);

        var item = new NutritionItems
        {
            AppUserId = command.AppUserId,
            Name = command.Request.Name!.Trim(),
            Type = command.Request.Type!.Value,
            Size = command.Request.Size,
            Carbs = command.Request.Carbs,
            Calories = command.Request.Calories,
            Caffeine = command.Request.Caffeine,
            Sodium = command.Request.Sodium,
            Potassium = command.Request.Potassium,
            Magnesium = command.Request.Magnesium,
            Calcium = command.Request.Calcium
        };

        await _nutritionItems.AddAsync(item, cancellationToken);
        await _nutritionItems.SaveChangesAsync(cancellationToken);

        return NutritionItemDto.FromEntity(item);
    }
}
