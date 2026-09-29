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
        var appUser = new AppUser { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "stored" };
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

        _repository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(appUser);
        _passwordHasher.Verify("password", "stored").Returns(true);
        _accessTokenIssuer.Issue(appUser.Id).Returns(new AccessToken("token", expiresAtUtc));
        _refreshTokenIssuer.Issue().Returns(new IssuedRefreshToken("refresh", "hash", expiresAtUtc.AddDays(30)));

        var result = await CreateHandler().Handle(
            new LoginUserCommand(new LoginUserRequest(" USER@example.com", "password")),
            CancellationToken.None);

        Assert.Equal("token", result.AccessToken.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        await _refreshTokensRepository.Received(1).AddAsync(
            Arg.Is<RefreshTokens>(t => t != null && t.AppUserId == appUser.Id && t.TokenHash == "hash"),
            Arg.Any<CancellationToken>());
        Assert.Equal(expiresAtUtc, result.AccessToken.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsUnauthorized()
    {
        var appUser = new AppUser { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "stored" };

        _repository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(appUser);
        _passwordHasher.Verify("wrong", "stored").Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(
                new LoginUserCommand(new LoginUserRequest("user@example.com", "wrong")),
                CancellationToken.None));

        _accessTokenIssuer.DidNotReceive().Issue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_StillVerifiesAPasswordAndThrowsUnauthorized()
    {
        _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(
                new LoginUserCommand(new LoginUserRequest("nobody@example.com", "password")),
                CancellationToken.None));

        _passwordHasher.Received(1).Verify("password", null);
        _accessTokenIssuer.DidNotReceive().Issue(Arg.Any<Guid>());
    }
}
