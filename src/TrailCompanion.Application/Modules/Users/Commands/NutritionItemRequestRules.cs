using TrailCompanion.Application.Modules.Users.Dtos;

namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class NutritionItemRequestRules
{
    public const int MaxNameLength = 200;

    public static void RequireValid(NutritionItemRequest request)
    {
        var failures = new List<ValidationFailure>();
        var name = request.Name?.Trim() ?? string.Empty;

        if (name.Length is 0 or > MaxNameLength)
        {
            failures.Add(new ValidationFailure(
                "InvalidName",
                $"Name is required and must be at most {MaxNameLength} characters."));
        }

        if (request.Type is null || !Enum.IsDefined(request.Type.Value))
        {
            failures.Add(new ValidationFailure("InvalidType", "Type must be one of Gel, Liquid, Solid or Pill."));
        }

        AddIfNegative(failures, "InvalidSize", "Size", request.Size);
        AddIfNegative(failures, "InvalidCarbs", "Carbs", request.Carbs);
        AddIfNegative(failures, "InvalidCalories", "Calories", request.Calories);
        AddIfNegative(failures, "InvalidCaffeine", "Caffeine", request.Caffeine);
        AddIfNegative(failures, "InvalidSodium", "Sodium", request.Sodium);
        AddIfNegative(failures, "InvalidPotassium", "Potassium", request.Potassium);
        AddIfNegative(failures, "InvalidMagnesium", "Magnesium", request.Magnesium);
        AddIfNegative(failures, "InvalidCalcium", "Calcium", request.Calcium);

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The nutrition item request is invalid.", failures);
        }
    }

    private static void AddIfNegative(List<ValidationFailure> failures, string code, string field, int? value)
    {
        if (value < 0)
        {
            failures.Add(new ValidationFailure(code, $"{field} must not be negative."));
        }
    }

    private static void AddIfNegative(List<ValidationFailure> failures, string code, string field, decimal? value)
    {
        if (value < 0)
        {
            failures.Add(new ValidationFailure(code, $"{field} must not be negative."));
        }
    }
}
