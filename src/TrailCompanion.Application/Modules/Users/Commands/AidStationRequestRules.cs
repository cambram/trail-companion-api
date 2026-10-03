using TrailCompanion.Application.Modules.Users.Dtos;

namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class AidStationRequestRules
{
    public const int MaxNameLength = 200;
    public const int MaxNotesLength = 2000;
    public const int MaxKitEntries = 30;
    public const int MaxKitLabelLength = 100;

    public static void RequireValid(AidStationRequest request)
    {
        var failures = new List<ValidationFailure>();
        var name = request.Name?.Trim() ?? string.Empty;
        var kit = request.Kit ?? [];

        if (name.Length is 0 or > MaxNameLength)
        {
            failures.Add(new ValidationFailure(
                "InvalidName",
                $"Name is required and must be at most {MaxNameLength} characters."));
        }

        if (request.Notes?.Length > MaxNotesLength)
        {
            failures.Add(new ValidationFailure(
                "InvalidNotes",
                $"Notes must be at most {MaxNotesLength} characters."));
        }

        if (kit.Count > MaxKitEntries)
        {
            failures.Add(new ValidationFailure(
                "InvalidKit",
                $"Kit must have at most {MaxKitEntries} entries."));
        }

        if (kit.Any(e => (e?.Label?.Trim().Length ?? 0) is 0 or > MaxKitLabelLength))
        {
            failures.Add(new ValidationFailure(
                "InvalidKitLabel",
                $"Each kit label is required and must be at most {MaxKitLabelLength} characters."));
        }

        if (kit.Any(e => e?.Certainty is not { } certainty || !Enum.IsDefined(certainty)))
        {
            failures.Add(new ValidationFailure("InvalidKitCertainty", "Each kit entry must be Planned or Maybe."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The aid station request is invalid.", failures);
        }
    }
}
