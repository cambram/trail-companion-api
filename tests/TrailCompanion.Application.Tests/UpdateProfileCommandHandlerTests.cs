using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class UpdateProfileCommandHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();
    private readonly AppUser _appUser = new() { Id = Guid.NewGuid(), Username = "runner", FirstName = "Old", LastName = "Name" };

    public UpdateProfileCommandHandlerTests()
    {
        _repository.GetByIdAsync(_appUser.Id, Arg.Any<CancellationToken>()).Returns(_appUser);
    }

    private UpdateProfileCommandHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_TrimsNamesAndStoresBlankAsNull()
    {
        var result = await CreateHandler().Handle(
            new UpdateProfileCommand(_appUser.Id, new UpdateProfileRequest("  Kilian ", "   ")),
            CancellationToken.None);

        Assert.Equal("Kilian", _appUser.FirstName);
        Assert.Null(_appUser.LastName);
        Assert.Equal("Kilian", result.FirstName);
        Assert.Equal("runner", result.Username);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(101, 1, "FirstNameTooLong")]
    [InlineData(1, 101, "LastNameTooLong")]
    public async Task Handle_WithAnOverlongName_ThrowsValidationFailedAndSavesNothing(int first, int last, string expectedCode)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => CreateHandler().Handle(
                new UpdateProfileCommand(_appUser.Id, new UpdateProfileRequest(new string('a', first), new string('b', last))),
                CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == expectedCode);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
