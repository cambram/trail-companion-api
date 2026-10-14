using TrailCompanion.Application.Modules.Users.Dtos;

namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class SegmentRequestRules
{
    public const int MaxNameLength = 200;
    public const int MaxNotesLength = 2000;
    public const int MaxAscentM = 10000;

    public static void RequireValid(SegmentRequest request)
    {
        var failures = new List<ValidationFailure>();
        var name = request.Name?.Trim() ?? string.Empty;

        if (name.Length is 0 or > MaxNameLength)
        {
            failures.Add(new ValidationFailure(
                "InvalidName",
                $"Name is required and must be at most {MaxNameLength} characters."));
        }

        if (request.Duration < 0)
        {
            failures.Add(new ValidationFailure("InvalidDuration", "Duration must not be negative."));
        }

        if (request.EndKm < 0)
        {
            failures.Add(new ValidationFailure("InvalidEndKm", "End km must not be negative."));
        }

        if (request.AscentM is < 0 or > MaxAscentM)
        {
            failures.Add(new ValidationFailure(
                "InvalidAscent",
                $"Ascent must be between 0 and {MaxAscentM} metres."));
        }

        if (request.Notes?.Length > MaxNotesLength)
        {
            failures.Add(new ValidationFailure(
                "InvalidNotes",
                $"Notes must be at most {MaxNotesLength} characters."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The segment request is invalid.", failures);
        }
    }
}
