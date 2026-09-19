namespace TrailCompanion.Application.Modules.Users.Dtos;

public record AccessTokenDto(
    string AccessToken,
    DateTime ExpiresAtUtc)
{
    public static AccessTokenDto FromAccessToken(AccessToken token) => new(
        token.Value,
        token.ExpiresAtUtc);
}
