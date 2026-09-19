namespace TrailCompanion.Application.Modules.Users;

internal static class UserEmail
{
    internal const int MaxLength = 320;

    internal static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
