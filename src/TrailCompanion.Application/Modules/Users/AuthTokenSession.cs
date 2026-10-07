using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;

namespace TrailCompanion.Application.Modules.Users;

internal static class AuthTokenSession
{
    internal static readonly TimeSpan MaxSessionLifetime = TimeSpan.FromDays(90);

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
            ExpiresAtUtc = refreshToken.ExpiresAtUtc,
            SessionStartedAtUtc = replacing?.SessionStartedAtUtc ?? DateTime.UtcNow
        };

        if (replacing is null)
        {
            await refreshTokensRepository.AddAsync(stored, cancellationToken);
            await refreshTokensRepository.SaveChangesAsync(cancellationToken);
        }
        else if (!await refreshTokensRepository.RotateAsync(replacing.Id, stored, cancellationToken))
        {
            await refreshTokensRepository.RevokeAllAsync(appUserId, null, cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used.");
        }

        return new AuthTokens(
            AccessTokenDto.FromAccessToken(accessTokenIssuer.Issue(appUserId)),
            refreshToken.Value,
            refreshToken.ExpiresAtUtc);
    }
}
