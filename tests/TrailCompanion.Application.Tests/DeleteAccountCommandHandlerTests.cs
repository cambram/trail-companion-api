using TrailCompanion.Application.Common.Contracts;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteAccountCommandHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly AppUser _appUser = new() { Id = Guid.NewGuid(), Username = "runner", PasswordHash = "stored" };

    public DeleteAccountCommandHandlerTests()
    {
        _repository.GetByIdAsync(_appUser.Id, Arg.Any<CancellationToken>()).Returns(_appUser);
        _passwordHasher.Verify("password", "stored").Returns(true);
    }

    private Task Delete(string? password) =>
        new DeleteAccountCommandHandler(_repository, _passwordHasher).Handle(
            new DeleteAccountCommand(_appUser.Id, new DeleteAccountRequest(password)),
            CancellationToken.None);

    [Fact]
    public async Task Handle_WithCorrectPassword_DeletesTheUser()
    {
        await Delete("password");

        await _repository.Received(1).DeleteAsync(_appUser.Id, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData(null)]
    public async Task Handle_WithWrongPassword_ThrowsUnauthorizedAndDeletesNothing(string? password)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Delete(password));

        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
