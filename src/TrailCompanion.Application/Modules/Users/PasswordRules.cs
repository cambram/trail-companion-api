namespace TrailCompanion.Application.Modules.Users;

internal static class PasswordRules
{
    internal const int MinimumLength = 8;

    internal static IEnumerable<ValidationFailure> Check(string password)
    {
        if (password.Length < MinimumLength)
        {
            yield return new ValidationFailure(
                "PasswordTooShort",
                $"Password must be at least {MinimumLength} characters.");
        }
    }
}
