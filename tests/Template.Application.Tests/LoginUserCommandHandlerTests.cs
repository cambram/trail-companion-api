using Template.Application.Common.Contracts;
using Template.Application.Modules.Users.Commands;
using Template.Application.Modules.Users.Dtos;
using Template.Domain.Common.Exceptions;
using Template.Domain.Modules.Users.Entities;
using Template.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace Template.Application.Tests;

public class LoginUserCommandHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAccessTokenIssuer _accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();

    private LoginUserCommandHandler CreateHandler() => new(_repository, _passwordHasher, _accessTokenIssuer);

    [Fact]
    public async Task Handle_WithCorrectPassword_IssuesATokenForThatUser()
    {
        var appUser = new AppUser { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "stored" };
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

        _repository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(appUser);
        _passwordHasher.Verify("password", "stored").Returns(true);
        _accessTokenIssuer.Issue(appUser.Id).Returns(new AccessToken("token", expiresAtUtc));

        var result = await CreateHandler().Handle(
            new LoginUserCommand(new LoginUserRequest(" USER@example.com", "password")),
            CancellationToken.None);

        Assert.Equal("token", result.AccessToken);
        Assert.Equal(expiresAtUtc, result.ExpiresAtUtc);
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
