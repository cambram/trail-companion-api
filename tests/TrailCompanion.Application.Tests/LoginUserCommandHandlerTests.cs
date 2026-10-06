using TrailCompanion.Application.Common.Contracts;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class LoginUserCommandHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAccessTokenIssuer _accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly IRefreshTokenIssuer _refreshTokenIssuer = Substitute.For<IRefreshTokenIssuer>();
    private readonly IRefreshTokensRepository _refreshTokensRepository = Substitute.For<IRefreshTokensRepository>();

    private LoginUserCommandHandler CreateHandler() => new(_repository, _passwordHasher, _accessTokenIssuer, _refreshTokenIssuer, _refreshTokensRepository);

    [Fact]
    public async Task Handle_WithCorrectPassword_IssuesATokenForThatUser()
    {
        var appUser = new AppUser { Id = Guid.NewGuid(), Username = "runner", PasswordHash = "stored" };
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

        _repository.GetByUsernameAsync("runner", Arg.Any<CancellationToken>()).Returns(appUser);
        _passwordHasher.Verify("password", "stored").Returns(true);
        _accessTokenIssuer.Issue(appUser.Id).Returns(new AccessToken("token", expiresAtUtc));
        _refreshTokenIssuer.Issue().Returns(new IssuedRefreshToken("refresh", "hash", expiresAtUtc.AddDays(30)));

        var result = await CreateHandler().Handle(
            new LoginUserCommand(new LoginUserRequest(" RUNNER", "password")),
            CancellationToken.None);

        Assert.Equal("token", result.AccessToken.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        await _refreshTokensRepository.Received(1).AddAsync(
            Arg.Is<RefreshTokens>(t => t != null && t.AppUserId == appUser.Id && t.TokenHash == "hash"),
            Arg.Any<CancellationToken>());
        Assert.Equal(expiresAtUtc, result.AccessToken.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_WithUsernameOutsideTheRules_StillLogsIn()
    {
        var appUser = new AppUser { Id = Guid.NewGuid(), Username = "first+tag", PasswordHash = "stored" };

        _repository.GetByUsernameAsync("first+tag", Arg.Any<CancellationToken>()).Returns(appUser);
        _passwordHasher.Verify("password", "stored").Returns(true);
        _accessTokenIssuer.Issue(appUser.Id).Returns(new AccessToken("token", DateTime.UtcNow.AddMinutes(15)));
        _refreshTokenIssuer.Issue().Returns(new IssuedRefreshToken("refresh", "hash", DateTime.UtcNow.AddDays(30)));

        var result = await CreateHandler().Handle(
            new LoginUserCommand(new LoginUserRequest("First+Tag", "password")),
            CancellationToken.None);

        Assert.Equal("token", result.AccessToken.AccessToken);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsUnauthorized()
    {
        var appUser = new AppUser { Id = Guid.NewGuid(), Username = "runner", PasswordHash = "stored" };

        _repository.GetByUsernameAsync("runner", Arg.Any<CancellationToken>()).Returns(appUser);
        _passwordHasher.Verify("wrong", "stored").Returns(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(
                new LoginUserCommand(new LoginUserRequest("runner", "wrong")),
                CancellationToken.None));

        Assert.Equal("Invalid username or password.", exception.Message);
        _accessTokenIssuer.DidNotReceive().Issue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WithUnknownUsername_StillVerifiesAPasswordAndThrowsUnauthorized()
    {
        _repository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(
                new LoginUserCommand(new LoginUserRequest("me@example.com", "password")),
                CancellationToken.None));

        Assert.Equal("Invalid username or password.", exception.Message);
        _passwordHasher.Received(1).Verify("password", null);
        _accessTokenIssuer.DidNotReceive().Issue(Arg.Any<Guid>());
    }
}
