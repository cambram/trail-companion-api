namespace TrailCompanion.Application.Common.Contracts;

public record IssuedRefreshToken(string Value, string Hash, DateTime ExpiresAtUtc);

public interface IRefreshTokenIssuer
{
    IssuedRefreshToken Issue();

    string Hash(string value);
}
