using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class RefreshAccessTokenCommand : IRequest<AuthTokens>
{
    public RefreshAccessTokenCommand(string? refreshToken)
    {
        RefreshToken = refreshToken;
    }

    public string? RefreshToken { get; }
}

internal sealed class RefreshAccessTokenCommandHandler : IRequestHandler<RefreshAccessTokenCommand, AuthTokens>
{
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IRefreshTokenIssuer _refreshTokenIssuer;
    private readonly IAccessTokenIssuer _accessTokenIssuer;

    public RefreshAccessTokenCommandHandler(
        IRefreshTokensRepository refreshTokensRepository,
        IRefreshTokenIssuer refreshTokenIssuer,
        IAccessTokenIssuer accessTokenIssuer)
    {
        _refreshTokensRepository = refreshTokensRepository;
        _refreshTokenIssuer = refreshTokenIssuer;
        _accessTokenIssuer = accessTokenIssuer;
    }

    public async Task<AuthTokens> Handle(RefreshAccessTokenCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            throw new UnauthorizedException("Refresh token is missing.");
        }

        var stored = await _refreshTokensRepository.GetByHashAsync(
            _refreshTokenIssuer.Hash(command.RefreshToken),
            cancellationToken);

        if (stored is null || stored.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        if (stored.RevokedAtUtc is not null)
        {
            await _refreshTokensRepository.RevokeAllAsync(stored.AppUserId, null, cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used.");
        }

        if (stored.SessionStartedAtUtc <= DateTime.UtcNow - AuthTokenSession.MaxSessionLifetime)
        {
            throw new UnauthorizedException("Session has expired. Log in again.");
        }

        return await AuthTokenSession.StartAsync(
            stored.AppUserId,
            stored,
            _accessTokenIssuer,
            _refreshTokenIssuer,
            _refreshTokensRepository,
            cancellationToken);
    }
}
