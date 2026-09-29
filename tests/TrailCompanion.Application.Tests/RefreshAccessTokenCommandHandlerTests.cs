using TrailCompanion.Application.Common.Contracts;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class RefreshAccessTokenCommandHandlerTests
{
    private readonly IRefreshTokensRepository _repository = Substitute.For<IRefreshTokensRepository>();
    private readonly IRefreshTokenIssuer _refreshTokenIssuer = Substitute.For<IRefreshTokenIssuer>();
    private readonly IAccessTokenIssuer _accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();

    public RefreshAccessTokenCommandHandlerTests()
    {
        _refreshTokenIssuer.Hash("presented").Returns("presented-hash");
        _refreshTokenIssuer.Issue().Returns(new IssuedRefreshToken("next", "next-hash", DateTime.UtcNow.AddDays(30)));
        _accessTokenIssuer.Issue(Arg.Any<Guid>()).Returns(new AccessToken("access", DateTime.UtcNow.AddMinutes(15)));
    }

    private RefreshAccessTokenCommandHandler CreateHandler() => new(_repository, _refreshTokenIssuer, _accessTokenIssuer);

    [Fact]
    public async Task Handle_WithAnActiveToken_RotatesIt()
    {
        var stored = StoreToken(DateTime.UtcNow.AddDays(1), revokedAtUtc: null);

        var result = await CreateHandler().Handle(new RefreshAccessTokenCommand("presented"), CancellationToken.None);

        Assert.Equal("next", result.RefreshToken);
        Assert.Equal("access", result.AccessToken.AccessToken);
        Assert.NotNull(stored.RevokedAtUtc);
        await _repository.Received(1).AddAsync(
            Arg.Is<RefreshTokens>(t => t != null && t.Id == stored.ReplacedByTokenId && t.TokenHash == "next-hash"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithASpentToken_RevokesEveryTokenForTheUser()
    {
        var stored = StoreToken(DateTime.UtcNow.AddDays(1), revokedAtUtc: DateTime.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(new RefreshAccessTokenCommand("presented"), CancellationToken.None));

        await _repository.Received(1).RevokeAllAsync(stored.AppUserId, null, Arg.Any<CancellationToken>());
        _refreshTokenIssuer.DidNotReceive().Issue();
    }

    [Fact]
    public async Task Handle_WithAnExpiredToken_ThrowsUnauthorized()
    {
        StoreToken(DateTime.UtcNow.AddSeconds(-1), revokedAtUtc: null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(new RefreshAccessTokenCommand("presented"), CancellationToken.None));

        _refreshTokenIssuer.DidNotReceive().Issue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task Handle_WithAMissingOrUnknownToken_ThrowsUnauthorized(string? token)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(new RefreshAccessTokenCommand(token), CancellationToken.None));

        _refreshTokenIssuer.DidNotReceive().Issue();
    }

    private RefreshTokens StoreToken(DateTime expiresAtUtc, DateTime? revokedAtUtc)
    {
        var stored = new RefreshTokens
        {
            Id = Guid.NewGuid(),
            AppUserId = Guid.NewGuid(),
            TokenHash = "presented-hash",
            ExpiresAtUtc = expiresAtUtc,
            RevokedAtUtc = revokedAtUtc
        };
        _repository.GetByHashAsync("presented-hash", Arg.Any<CancellationToken>()).Returns(stored);
        return stored;
    }
}
