using Template.Application.Modules.Users.Queries;
using Template.Domain.Common.Exceptions;
using Template.Domain.Modules.Users.Entities;
using Template.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace Template.Application.Tests;

public class GetCurrentAppUserQueryHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();

    private GetCurrentAppUserQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_WhenTheRecordExists_ReturnsIt()
    {
        var appUserId = Guid.NewGuid();
        var stored = new AppUser { Id = appUserId, Email = "user@example.com" };

        _repository.GetByIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(stored);

        var result = await CreateHandler().Handle(
            new GetCurrentAppUserQuery(appUserId), CancellationToken.None);

        Assert.Equal(appUserId, result.Id);
        Assert.Equal("user@example.com", result.Email);
    }

    [Fact]
    public async Task Handle_WhenTheRecordIsMissing_ThrowsNotFound()
    {
        var appUserId = Guid.NewGuid();

        _repository.GetByIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateHandler().Handle(new GetCurrentAppUserQuery(appUserId), CancellationToken.None));
    }
}
