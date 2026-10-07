using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class LoginUserCommand : IRequest<AuthTokens>
{
    public LoginUserCommand(LoginUserRequest request)
    {
        Request = request;
    }

    public LoginUserRequest Request { get; }
}

internal sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthTokens>
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IRefreshTokenIssuer _refreshTokenIssuer;
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly LoginThrottle _loginThrottle;

    public LoginUserCommandHandler(
        IAppUserRepository appUserRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenIssuer refreshTokenIssuer,
        IRefreshTokensRepository refreshTokensRepository,
        LoginThrottle loginThrottle)
    {
        _appUserRepository = appUserRepository;
        _passwordHasher = passwordHasher;
        _accessTokenIssuer = accessTokenIssuer;
        _refreshTokenIssuer = refreshTokenIssuer;
        _refreshTokensRepository = refreshTokensRepository;
        _loginThrottle = loginThrottle;
    }

    public async Task<AuthTokens> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var username = UserName.Normalize(command.Request.Username);

        var throttled = UserName.IsValid(username);

        if (throttled && _loginThrottle.IsBlocked(username))
        {
            throw new UnauthorizedException("Too many failed attempts. Try again in 15 minutes.");
        }

        var appUser = await _appUserRepository.GetByUsernameAsync(username, cancellationToken);

        var passwordMatches = _passwordHasher.Verify(command.Request.Password ?? string.Empty, appUser?.PasswordHash);

        if (appUser is null || !passwordMatches)
        {
            if (throttled)
            {
                _loginThrottle.RecordFailure(username);
            }

            throw new UnauthorizedException("Invalid username or password.");
        }

        if (throttled)
        {
            _loginThrottle.Reset(username);
        }

        return await AuthTokenSession.StartAsync(
            appUser.Id,
            null,
            _accessTokenIssuer,
            _refreshTokenIssuer,
            _refreshTokensRepository,
            cancellationToken);
    }
}
