namespace TrailCompanion.Application.Modules.Users.Commands;

internal static class AccountLimitRules
{
    public const int MaxRaces = 100;
    public const int MaxSegmentsPerRace = 100;
    public const int MaxItemsPerSegment = 50;
    public const int MaxItemsPerAidStation = 50;
    public const int MaxNutritionItems = 500;
    public const int MaxCrewPerRace = 20;

    public static void RequireRoom(int existing, int max, string what)
    {
        if (existing >= max)
        {
            throw new ValidationFailedException(
                "The request exceeds an account limit.",
                [new ValidationFailure("LimitReached", $"You can have at most {max} {what}.")]);
        }
    }
}
