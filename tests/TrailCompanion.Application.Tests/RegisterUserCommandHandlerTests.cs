using TrailCompanion.Application.Common.Contracts;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class RegisterUserCommandHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private RegisterUserCommandHandler CreateHandler() => new(_repository, _passwordHasher);

    [Fact]
    public async Task Handle_WithValidRequest_StoresNormalisedEmailAndHashedPassword()
    {
        _passwordHasher.Hash("correct horse battery").Returns("hashed");

        var result = await CreateHandler().Handle(
            new RegisterUserCommand(new RegisterUserRequest("  User@Example.COM ", "correct horse battery")),
            CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<AppUser>(u => u != null && u.Email == "user@example.com" && u.PasswordHash == "hashed"),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("user@example.com", result.Email);
    }

    [Theory]
    [InlineData(null, "correct horse battery", "InvalidEmail")]
    [InlineData("not-an-email", "correct horse battery", "InvalidEmail")]
    [InlineData("user@example.com", "short", "PasswordTooShort")]
    [InlineData("user@example.com", null, "PasswordTooShort")]
    public async Task Handle_WithInvalidRequest_ThrowsValidationFailedAndStoresNothing(
        string? email, string? password, string expectedCode)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => CreateHandler().Handle(
                new RegisterUserCommand(new RegisterUserRequest(email, password)),
                CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == expectedCode);
        await _repository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
    }
}
