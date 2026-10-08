using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using TrailCompanion.Domain.Modules.Users.Repositories;

namespace TrailCompanion.Application.Modules.Users;

internal static class RacePins
{
    public const int MaxAttempts = 10;

    public static string Normalize(string? pin) => (pin ?? string.Empty).Replace(" ", string.Empty);

    public static bool IsValid([NotNullWhen(true)] string? pin) =>
        pin is { Length: 6 } && pin.All(char.IsAsciiDigit);

    public static async Task<string> GenerateUniqueAsync(IRacesRepository races, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var pin = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            if (!await races.PinExistsAsync(pin, cancellationToken))
            {
                return pin;
            }
        }

        throw new ConflictException("PinTaken", "Could not assign a race PIN. Try again.");
    }
}
