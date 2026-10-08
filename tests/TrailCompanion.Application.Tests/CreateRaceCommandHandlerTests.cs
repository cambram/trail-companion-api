using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class CreateRaceCommandHandlerTests
{
    private readonly IRacesRepository _repository = Substitute.For<IRacesRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();

    private CreateRaceCommandHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_WithNameOnly_StoresTrimmedNameOwnedByUserWithDefaultTarget()
    {
        var result = await CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("  UTMB ", null)),
            CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Races>(r => r != null && r.AppUserId == _appUserId && r.Name == "UTMB" && r.Target == 60),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("UTMB", result.Name);
        Assert.Equal(60, result.Target);
    }

    [Fact]
    public async Task Handle_WithTarget_StoresTarget()
    {
        var result = await CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("UTMB", 90)),
            CancellationToken.None);

        Assert.Equal(90, result.Target);
    }

    [Fact]
    public async Task Handle_WithRaceDate_StoresRaceDate_AndDefaultsToNull()
    {
        var dated = await CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("UTMB", null, null, new DateOnly(2026, 10, 4))),
            CancellationToken.None);
        var undated = await CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("UTMB", null)),
            CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 10, 4), dated.RaceDate);
        Assert.Null(undated.RaceDate);
    }

    [Theory]
    [InlineData(null, null, "InvalidName")]
    [InlineData("   ", null, "InvalidName")]
    [InlineData("UTMB", -1, "InvalidTarget")]
    public async Task Handle_WithInvalidRequest_ThrowsValidationFailedAndStoresNothing(
        string? name, int? target, string expectedCode)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => CreateHandler().Handle(
                new CreateRaceCommand(_appUserId, new CreateRaceRequest(name, target)),
                CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == expectedCode);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Races>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AtTheRaceLimit_ThrowsAndStoresNothing()
    {
        _repository.GetAllAsync(_appUserId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, AccountLimitRules.MaxRaces).Select(_ => new Races()).ToList());

        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("UTMB", null)),
            CancellationToken.None));

        await _repository.DidNotReceive().AddAsync(Arg.Any<Races>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AssignsASixDigitPin()
    {
        await CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("UTMB", null)),
            CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Races>(r => r != null && r.Pin.Length == 6 && r.Pin.All(char.IsAsciiDigit)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAGeneratedPinIsTaken_TriesAnother()
    {
        _repository.PinExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true, false);

        await CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("UTMB", null)),
            CancellationToken.None);

        await _repository.Received(2).PinExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(Arg.Any<Races>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEveryAttemptIsTaken_ThrowsConflictAndStoresNothing()
    {
        _repository.PinExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => CreateHandler().Handle(
            new CreateRaceCommand(_appUserId, new CreateRaceRequest("UTMB", null)),
            CancellationToken.None));

        Assert.Equal("PinTaken", exception.Code);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Races>(), Arg.Any<CancellationToken>());
    }
}
