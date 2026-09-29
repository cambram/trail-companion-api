using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class LogoutCommand : IRequest
{
    public LogoutCommand(string? refreshToken)
    {
        RefreshToken = refreshToken;
    }

    public string? RefreshToken { get; }
}

internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IRefreshTokenIssuer _refreshTokenIssuer;

    public LogoutCommandHandler(IRefreshTokensRepository refreshTokensRepository, IRefreshTokenIssuer refreshTokenIssuer)
    {
        _refreshTokensRepository = refreshTokensRepository;
        _refreshTokenIssuer = refreshTokenIssuer;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return;
        }

        var stored = await _refreshTokensRepository.GetByHashAsync(
            _refreshTokenIssuer.Hash(command.RefreshToken),
            cancellationToken);

        if (stored is not { RevokedAtUtc: null })
        {
            return;
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);
    }
}
