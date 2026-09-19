using TrailCompanion.Application.Modules.Users.Dtos;

namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class ReorderRequestRules
{
    public static IReadOnlyList<Guid> RequireIds(ReorderRequest request)
    {
        var ids = request.Ids;

        if (ids is null || ids.Distinct().Count() != ids.Count)
        {
            throw Invalid("Ids must be a list without duplicates.");
        }

        return ids;
    }

    public static void RequireComplete(int provided, int existing)
    {
        if (provided != existing)
        {
            throw Invalid("Ids must list every existing entry exactly once.");
        }
    }

    private static ValidationFailedException Invalid(string message) =>
        new("The reorder request is invalid.", new List<ValidationFailure> { new("InvalidOrder", message) });
}
