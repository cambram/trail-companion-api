using TrailCompanion.Application.Common.Contracts;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class ChangePasswordCommandHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();
    private readonly IRefreshTokensRepository _refreshTokensRepository = Substitute.For<IRefreshTokensRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IRefreshTokenIssuer _refreshTokenIssuer = Substitute.For<IRefreshTokenIssuer>();
    private readonly AppUser _appUser = new() { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "stored" };

    public ChangePasswordCommandHandlerTests()
    {
        _repository.GetByIdAsync(_appUser.Id, Arg.Any<CancellationToken>()).Returns(_appUser);
        _passwordHasher.Verify("current password", "stored").Returns(true);
        _passwordHasher.Hash("brand new password").Returns("rehashed");
        _refreshTokenIssuer.Hash("cookie").Returns("cookie-hash");
    }

    private ChangePasswordCommandHandler CreateHandler() =>
        new(_repository, _refreshTokensRepository, _passwordHasher, _refreshTokenIssuer);

    private Task Change(string? current, string? next) =>
        CreateHandler().Handle(
            new ChangePasswordCommand(_appUser.Id, new ChangePasswordRequest(current, next), "cookie"),
            CancellationToken.None);

    [Fact]
    public async Task Handle_WithCorrectPassword_RehashesAndRevokesOtherRefreshTokens()
    {
        await Change("current password", "brand new password");

        Assert.Equal("rehashed", _appUser.PasswordHash);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokensRepository.Received(1).RevokeAllAsync(_appUser.Id, "cookie-hash", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ThrowsUnauthorizedAndChangesNothing()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Change("wrong", "brand new password"));

        Assert.Equal("stored", _appUser.PasswordHash);
        await _refreshTokensRepository.DidNotReceive().RevokeAllAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("short", "PasswordTooShort")]
    [InlineData(null, "PasswordTooShort")]
    [InlineData("current password", "PasswordUnchanged")]
    public async Task Handle_WithAnInvalidNewPassword_ThrowsValidationFailed(string? next, string expectedCode)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Change("current password", next));

        Assert.Contains(exception.Failures, f => f.Code == expectedCode);
        Assert.Equal("stored", _appUser.PasswordHash);
    }
}
