using TrailCompanion.Application.Common.Contracts;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class LogoutCommandHandlerTests
{
    private readonly IRefreshTokensRepository _repository = Substitute.For<IRefreshTokensRepository>();
    private readonly IRefreshTokenIssuer _refreshTokenIssuer = Substitute.For<IRefreshTokenIssuer>();

    private LogoutCommandHandler CreateHandler() => new(_repository, _refreshTokenIssuer);

    [Fact]
    public async Task Handle_WithAnActiveToken_RevokesIt()
    {
        var stored = new RefreshTokens { TokenHash = "hash" };
        _refreshTokenIssuer.Hash("presented").Returns("hash");
        _repository.GetByHashAsync("hash", Arg.Any<CancellationToken>()).Returns(stored);

        await CreateHandler().Handle(new LogoutCommand("presented"), CancellationToken.None);

        Assert.NotNull(stored.RevokedAtUtc);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutAToken_DoesNothing()
    {
        await CreateHandler().Handle(new LogoutCommand(null), CancellationToken.None);

        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
