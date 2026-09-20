using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record ItemDto(
    Guid Id,
    Guid SegmentId,
    Guid NutritionItemId,
    int Position,
    string Name,
    NutritionType Type,
    int? Size,
    int? Carbs,
    int? Calories,
    int? Caffeine,
    decimal? Sodium,
    decimal? Potassium,
    decimal? Magnesium,
    decimal? Calcium)
{
    public static ItemDto FromEntity(Items item, NutritionItems nutritionItem) => new(
        item.Id,
        item.SegmentId,
        item.NutritionItemId,
        item.Position,
        nutritionItem.Name,
        nutritionItem.Type,
        nutritionItem.Size,
        nutritionItem.Carbs,
        nutritionItem.Calories,
        nutritionItem.Caffeine,
        nutritionItem.Sodium,
        nutritionItem.Potassium,
        nutritionItem.Magnesium,
        nutritionItem.Calcium);
}
