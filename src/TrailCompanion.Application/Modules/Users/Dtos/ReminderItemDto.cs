using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record ReminderItemDto(
    Guid Id,
    int ReminderNumber,
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
    public static ReminderItemDto FromEntity(ReminderItems item, NutritionItems nutritionItem) => new(
        item.Id,
        item.ReminderNumber,
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
