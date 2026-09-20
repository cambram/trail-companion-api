using System.Collections.Frozen;

namespace TrailCompanion.Infrastructure.Common.Persistence;

internal static class UniqueConstraintConflicts
{
    private static readonly FrozenDictionary<string, (string Code, string Message)> ByConstraintName =
        new Dictionary<string, (string Code, string Message)>
        {
            ["IX_AppUsers_Email"] =
                ("DuplicateEmail", "An account with this email already exists."),
            ["IX_NutritionItems_AppUserId_Name"] =
                ("DuplicateNutritionItem", "You already have a nutrition item with this name."),
        }.ToFrozenDictionary();

    public static bool TryTranslate(string? constraintName, out (string Code, string Message) conflict)
    {
        if (constraintName is null)
        {
            conflict = default;
            return false;
        }

        return ByConstraintName.TryGetValue(constraintName, out conflict);
    }
}
