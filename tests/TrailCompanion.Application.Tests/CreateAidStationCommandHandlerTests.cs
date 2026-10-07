using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class CreateAidStationCommandHandlerTests
{
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly IAidStationsRepository _stations = Substitute.For<IAidStationsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _segmentId = Guid.NewGuid();

    private Task<AidStationDto> Send(Guid? segmentId, string? name) =>
        new CreateAidStationCommandHandler(_segments, _stations).Handle(
            new CreateAidStationCommand(_appUserId, _raceId, new CreateAidStationRequest(segmentId, name)),
            CancellationToken.None);

    private void GivenOwnedSegment() =>
        _segments.GetByIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>())
            .Returns(new Segments { Id = _segmentId, RaceId = _raceId, Name = "S" });

    [Fact]
    public async Task Handle_OnFreeSegment_CreatesStationWithTrimmedNameAndEmptyKit()
    {
        GivenOwnedSegment();

        var result = await Send(_segmentId, " Hout Bay ");

        Assert.Equal(_segmentId, result.SegmentId);
        Assert.Equal("Hout Bay", result.Name);
        Assert.Equal(string.Empty, result.Notes);
        Assert.Empty(result.Kit);
        await _stations.Received(1).AddAsync(Arg.Is<AidStations>(a => a!.SegmentId == _segmentId), Arg.Any<CancellationToken>());
        await _stations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSegmentThatHasAStation_ThrowsConflict()
    {
        GivenOwnedSegment();
        _stations.GetBySegmentIdAsync(_appUserId, _raceId, _segmentId, Arg.Any<CancellationToken>())
            .Returns(new AidStations { SegmentId = _segmentId, Name = "Taken" });

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Send(_segmentId, "A"));

        Assert.Equal("SegmentHasAidStation", exception.Code);
        await _stations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownSegment_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Send(_segmentId, "A"));
    }

    [Fact]
    public async Task Handle_WithoutSegmentOrName_ThrowsValidation()
    {
        GivenOwnedSegment();

        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(null, "A"));
        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(_segmentId, " "));
    }
}
