using TrailCompanion.Application.Modules.Users.Dtos;

namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class ItemRequestRules
{
    public const int MaxNameLength = 200;

    public static void RequireValid(ItemRequest request)
    {
        var failures = new List<ValidationFailure>();
        var name = request.Name?.Trim() ?? string.Empty;

        if (name.Length is 0 or > MaxNameLength)
        {
            failures.Add(new ValidationFailure(
                "InvalidName",
                $"Name is required and must be at most {MaxNameLength} characters."));
        }

        if (request.Carbs < 0)
        {
            failures.Add(new ValidationFailure("InvalidCarbs", "Carbs must not be negative."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The item request is invalid.", failures);
        }
    }
}
