using System.Text.RegularExpressions;

namespace TrailCompanion.Application.Modules.Users;

internal static partial class UserName
{
    internal static string Normalize(string? username) => (username ?? string.Empty).Trim().ToLowerInvariant();

    internal static bool IsValid(string normalized) => Pattern().IsMatch(normalized);

    [GeneratedRegex("^[a-z0-9_.-]{3,32}$")]
    private static partial Regex Pattern();
}
