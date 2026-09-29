namespace TrailCompanion.Application.Modules.Users.Dtos;

public record AuthTokens(
    AccessTokenDto AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
