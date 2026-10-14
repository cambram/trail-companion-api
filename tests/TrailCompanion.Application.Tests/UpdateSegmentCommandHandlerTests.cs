using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class UpdateSegmentCommandHandlerTests
{
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();

    private UpdateSegmentCommandHandler CreateHandler() => new(_segments);

    private Segments GivenSegment()
    {
        var segment = new Segments
        {
            Id = Guid.NewGuid(),
            RaceId = _raceId,
            Position = 3,
            Name = "Old",
            Duration = 10,
            EndKm = 1,
            AscentM = 50,
            Notes = "old notes"
        };
        _segments.GetByIdAsync(_appUserId, _raceId, segment.Id, Arg.Any<CancellationToken>()).Returns(segment);
        return segment;
    }

    [Fact]
    public async Task Handle_ReplacesEditableFieldsAndKeepsPosition()
    {
        var segment = GivenSegment();

        var result = await CreateHandler().Handle(
            new UpdateSegmentCommand(_appUserId, _raceId, segment.Id, new SegmentRequest(" New ", 45, null, null)),
            CancellationToken.None);

        Assert.Equal("New", result.Name);
        Assert.Equal(45, result.Duration);
        Assert.Null(result.EndKm);
        Assert.Equal(string.Empty, result.Notes);
        Assert.Equal(3, result.Position);
        await _segments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReplacesAscent_AndClearsItWhenOmitted()
    {
        var segment = GivenSegment();

        var set = await CreateHandler().Handle(
            new UpdateSegmentCommand(_appUserId, _raceId, segment.Id, new SegmentRequest("New", 45, null, null, 900)),
            CancellationToken.None);
        var cleared = await CreateHandler().Handle(
            new UpdateSegmentCommand(_appUserId, _raceId, segment.Id, new SegmentRequest("New", 45, null, null)),
            CancellationToken.None);

        Assert.Equal(900, set.AscentM);
        Assert.Null(cleared.AscentM);
    }

    [Fact]
    public async Task Handle_WhenSegmentMissingOrNotOwned_ThrowsNotFoundAndSavesNothing()
    {
        var segmentId = Guid.NewGuid();
        _segments.GetByIdAsync(_appUserId, _raceId, segmentId, Arg.Any<CancellationToken>()).Returns((Segments?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler().Handle(
            new UpdateSegmentCommand(_appUserId, _raceId, segmentId, new SegmentRequest("A", 1, null, null)),
            CancellationToken.None));

        await _segments.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidInput_ThrowsValidationFailedAndLeavesSegmentUntouched()
    {
        var segment = GivenSegment();

        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateHandler().Handle(
            new UpdateSegmentCommand(_appUserId, _raceId, segment.Id, new SegmentRequest("", -5, null, null)),
            CancellationToken.None));

        Assert.Equal("Old", segment.Name);
        await _segments.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
