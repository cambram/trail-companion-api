using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;

namespace TrailCompanion.Api.Common.Services;

internal static class RefreshTokenCookie
{
    public const string Name = "refresh_token";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Write(HttpResponse response, AuthTokens tokens) =>
        response.Cookies.Append(Name, tokens.RefreshToken, Options(tokens.RefreshTokenExpiresAtUtc));

    public static void Clear(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(DateTime.UnixEpoch));

    private static CookieOptions Options(DateTime expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = $"/{ApiEndpoints.Users.UserBase}",
        Expires = expiresAtUtc
    };
}
