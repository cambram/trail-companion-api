namespace TrailCompanion.Application.Common.Contracts;

public record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface IAccessTokenIssuer
{
    AccessToken Issue(Guid appUserId);
}
