using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;

namespace TrailCompanion.Application.Modules.Users;

internal static class AuthTokenSession
{
    internal static async Task<AuthTokens> StartAsync(
        Guid appUserId,
        RefreshTokens? replacing,
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenIssuer refreshTokenIssuer,
        IRefreshTokensRepository refreshTokensRepository,
        CancellationToken cancellationToken)
    {
        var refreshToken = refreshTokenIssuer.Issue();
        var stored = new RefreshTokens
        {
            Id = Guid.NewGuid(),
            AppUserId = appUserId,
            TokenHash = refreshToken.Hash,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        };

        if (replacing is not null)
        {
            replacing.RevokedAtUtc = DateTime.UtcNow;
            replacing.ReplacedByTokenId = stored.Id;
        }

        await refreshTokensRepository.AddAsync(stored, cancellationToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            AccessTokenDto.FromAccessToken(accessTokenIssuer.Issue(appUserId)),
            refreshToken.Value,
            refreshToken.ExpiresAtUtc);
    }
}
