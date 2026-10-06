using TrailCompanion.Application.Common.Constants;
using Microsoft.FeatureManagement;

namespace TrailCompanion.Application.Modules.Users;

internal static class PasswordRules
{
    internal const int MinimumLength = 8;
    internal const int MaximumLength = 128;

    internal static IEnumerable<ValidationFailure> Check(string password)
    {
        if (password.Length < MinimumLength)
        {
            yield return new ValidationFailure(
                "PasswordTooShort",
                $"Password must be at least {MinimumLength} characters.");
        }

        if (password.Length > MaximumLength)
        {
            yield return new ValidationFailure(
                "PasswordTooLong",
                $"Password must be at most {MaximumLength} characters.");
        }
    }

    internal static async Task RequireNotBreachedAsync(
        string password,
        IVariantFeatureManager featureManager,
        IBreachedPasswordChecker checker,
        string message,
        CancellationToken cancellationToken)
    {
        if (await featureManager.IsEnabledAsync(FeatureFlags.BreachedPasswordCheck, cancellationToken)
            && await checker.IsBreachedAsync(password, cancellationToken))
        {
            throw new ValidationFailedException(
                message,
                [new ValidationFailure("PasswordBreached", "This password appears in a known data breach. Choose a different one.")]);
        }
    }
}
