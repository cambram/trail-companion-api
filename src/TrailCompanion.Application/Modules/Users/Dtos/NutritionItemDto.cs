using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record NutritionItemDto(
    Guid Id,
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
    public static NutritionItemDto FromEntity(NutritionItems item) => new(
        item.Id,
        item.Name,
        item.Type,
        item.Size,
        item.Carbs,
        item.Calories,
        item.Caffeine,
        item.Sodium,
        item.Potassium,
        item.Magnesium,
        item.Calcium);
}
