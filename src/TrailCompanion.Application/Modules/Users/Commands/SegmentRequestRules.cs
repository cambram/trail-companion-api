using TrailCompanion.Application.Modules.Users.Dtos;

namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class SegmentRequestRules
{
    public const int MaxNameLength = 200;
    public const int MaxNotesLength = 2000;

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

        if (request.Distance < 0)
        {
            failures.Add(new ValidationFailure("InvalidDistance", "Distance must not be negative."));
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
