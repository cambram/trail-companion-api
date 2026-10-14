using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class CreateSegmentCommandHandlerTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();

    private CreateSegmentCommandHandler CreateHandler() => new(_races, _segments);

    private void GivenOwnedRace(params Segments[] existing)
    {
        _races.GetByIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>())
            .Returns(new Races { Id = _raceId, AppUserId = _appUserId, Name = "UTMB" });
        _segments.GetByRaceIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>()).Returns(existing);
    }

    [Fact]
    public async Task Handle_OnEmptyRace_CreatesSegmentAtPositionOne()
    {
        GivenOwnedRace();

        var result = await CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("  Start to Aid 1 ", 90, 11.5, "Easy")),
            CancellationToken.None);

        Assert.Equal(1, result.Position);
        Assert.Equal("Start to Aid 1", result.Name);
        Assert.Equal(90, result.Duration);
        Assert.Equal(11.5, result.EndKm);
        Assert.Equal("Easy", result.Notes);
        await _segments.Received(1).AddAsync(Arg.Is<Segments>(s => s!.RaceId == _raceId), Arg.Any<CancellationToken>());
        await _segments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StoresAscentAndLeavesItNullWhenOmitted()
    {
        GivenOwnedRace();

        var withAscent = await CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("A", 90, 11.5, null, 600)),
            CancellationToken.None);
        var without = await CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("B", 90, 11.5, null)),
            CancellationToken.None);

        Assert.Equal(600, withAscent.AscentM);
        Assert.Null(without.AscentM);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10001)]
    public async Task Handle_WithAscentOutOfRange_ThrowsInvalidAscentAndSavesNothing(int ascent)
    {
        GivenOwnedRace();

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("A", 90, null, null, ascent)),
            CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidAscent");
        await _segments.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingSegments_AppendsAfterHighestPosition()
    {
        GivenOwnedRace(
            new Segments { Position = 1, Name = "A" },
            new Segments { Position = 4, Name = "B" });

        var result = await CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("C", null, null, null)),
            CancellationToken.None);

        Assert.Equal(5, result.Position);
        Assert.Equal(0, result.Duration);
        Assert.Null(result.EndKm);
        Assert.Equal(string.Empty, result.Notes);
    }

    [Fact]
    public async Task Handle_WhenRaceMissingOrNotOwned_ThrowsNotFoundAndSavesNothing()
    {
        _races.GetByIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>()).Returns((Races?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("A", 1, null, null)),
            CancellationToken.None));

        await _segments.DidNotReceive().AddAsync(Arg.Any<Segments>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, 10, null)]
    [InlineData("   ", 10, null)]
    [InlineData("A", -1, null)]
    [InlineData("A", 10, -0.5)]
    public async Task Handle_WithInvalidInput_ThrowsValidationFailed(string? name, int duration, double? endKm)
    {
        GivenOwnedRace();

        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest(name, duration, endKm, null)),
            CancellationToken.None));

        await _segments.DidNotReceive().AddAsync(Arg.Any<Segments>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTooLongNameOrNotes_ThrowsValidationFailed()
    {
        GivenOwnedRace();

        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest(new string('a', 201), 1, null, null)),
            CancellationToken.None));
        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("A", 1, null, new string('n', 2001))),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AtTheSegmentLimit_ThrowsAndStoresNothing()
    {
        GivenOwnedRace(Enumerable.Range(1, AccountLimitRules.MaxSegmentsPerRace)
            .Select(i => new Segments { Position = i, Name = "S" })
            .ToArray());

        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new CreateSegmentCommand(_appUserId, _raceId, new SegmentRequest("Next", 60, null, null)),
            CancellationToken.None));

        await _segments.DidNotReceive().AddAsync(Arg.Any<Segments>(), Arg.Any<CancellationToken>());
    }
}
