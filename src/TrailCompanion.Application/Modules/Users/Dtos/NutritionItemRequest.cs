using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record NutritionItemRequest(
    string? Name,
    NutritionType? Type,
    int? Size,
    int? Carbs,
    int? Calories,
    int? Caffeine,
    decimal? Sodium,
    decimal? Potassium,
    decimal? Magnesium,
    decimal? Calcium);
