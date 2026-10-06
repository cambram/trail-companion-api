using TrailCompanion.Application.Modules.Users.Queries;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class GetCurrentAppUserQueryHandlerTests
{
    private readonly IAppUserRepository _repository = Substitute.For<IAppUserRepository>();

    private GetCurrentAppUserQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_WhenTheRecordExists_ReturnsIt()
    {
        var appUserId = Guid.NewGuid();
        var stored = new AppUser { Id = appUserId, Username = "runner" };

        _repository.GetByIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(stored);

        var result = await CreateHandler().Handle(
            new GetCurrentAppUserQuery(appUserId), CancellationToken.None);

        Assert.Equal(appUserId, result.Id);
        Assert.Equal("runner", result.Username);
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
