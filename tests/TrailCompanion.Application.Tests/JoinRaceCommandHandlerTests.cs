using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace TrailCompanion.Application.Tests;

public class JoinRaceCommandHandlerTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly IRaceCrewMembersRepository _crew = Substitute.For<IRaceCrewMembersRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Races _race = new() { Id = Guid.NewGuid(), AppUserId = Guid.NewGuid(), Name = "UTCT", Pin = "482913" };

    public JoinRaceCommandHandlerTests()
    {
        _races.GetByPinAsync("482913", Arg.Any<CancellationToken>()).Returns(_race);
    }

    private JoinRaceCommandHandler CreateHandler() => new(_races, _crew);

    private Task<RaceDto> Join(string? pin) =>
        CreateHandler().Handle(new JoinRaceCommand(_appUserId, new JoinRaceRequest(pin)), CancellationToken.None);

    [Fact]
    public async Task Handle_WithAValidPin_AddsTheCallerAsCrewAndReturnsCrewRole()
    {
        var result = await Join("482913");

        Assert.Equal(_race.Id, result.Id);
        Assert.Equal(RaceRole.Crew, result.Role);
        await _crew.Received(1).AddAsync(
            Arg.Is<RaceCrewMembers>(m => m != null && m.RaceId == _race.Id && m.AppUserId == _appUserId),
            Arg.Any<CancellationToken>());
        await _crew.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSpacesInThePin_StillJoins()
    {
        var result = await Join(" 482 913 ");

        Assert.Equal(_race.Id, result.Id);
    }

    [Fact]
    public async Task Handle_WhenAlreadyCrew_SucceedsWithoutAddingAgain()
    {
        _crew.GetAsync(_race.Id, _appUserId, Arg.Any<CancellationToken>())
            .Returns(new RaceCrewMembers { RaceId = _race.Id, AppUserId = _appUserId });

        var result = await Join("482913");

        Assert.Equal(RaceRole.Crew, result.Role);
        await _crew.DidNotReceive().AddAsync(Arg.Any<RaceCrewMembers>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAConcurrentJoinWinsTheInsert_StillSucceeds()
    {
        _crew.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("AlreadyCrew", "You are already on this race's crew."));

        var result = await Join("482913");

        Assert.Equal(RaceRole.Crew, result.Role);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    public async Task Handle_WithABadPin_ThrowsInvalidPinAndLooksNothingUp(string? pin)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Join(pin));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidPin");
        await _races.DidNotReceive().GetByPinAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnknownPin_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Join("000000"));
    }

    [Fact]
    public async Task Handle_ForTheOwner_ThrowsOwnRace()
    {
        _race.AppUserId = _appUserId;

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Join("482913"));

        Assert.Contains(exception.Failures, f => f.Code == "OwnRace");
        await _crew.DidNotReceive().AddAsync(Arg.Any<RaceCrewMembers>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheCrewIsFull_ThrowsLimitReached()
    {
        _crew.CountByRaceIdAsync(_race.Id, Arg.Any<CancellationToken>()).Returns(AccountLimitRules.MaxCrewPerRace);

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Join("482913"));

        Assert.Contains(exception.Failures, f => f.Code == "LimitReached");
    }
}
