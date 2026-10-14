namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class ReminderRequestRules
{
    public const int MinNumber = 1;
    public const int MaxNumber = 500;

    public static void RequireNumber(int number)
    {
        if (number is < MinNumber or > MaxNumber)
        {
            throw new ValidationFailedException(
                "The reminder request is invalid.",
                [new ValidationFailure(
                    "InvalidReminderNumber",
                    $"Reminder number must be between {MinNumber} and {MaxNumber}.")]);
        }
    }
}
