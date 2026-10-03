using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class MoveAidStationCommandHandlerTests
{
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly IAidStationsRepository _stations = Substitute.For<IAidStationsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly Guid _fromSegmentId = Guid.NewGuid();
    private readonly Guid _toSegmentId = Guid.NewGuid();
    private readonly AidStations _station;

    public MoveAidStationCommandHandlerTests()
    {
        _station = new AidStations { Id = Guid.NewGuid(), SegmentId = _fromSegmentId, Name = "Moving" };
        _stations.GetByIdAsync(_appUserId, _raceId, _station.Id, Arg.Any<CancellationToken>()).Returns(_station);
        _segments.GetByIdAsync(_appUserId, _raceId, _toSegmentId, Arg.Any<CancellationToken>())
            .Returns(new Segments { Id = _toSegmentId, RaceId = _raceId, Name = "To" });
        _segments.GetByIdAsync(_appUserId, _raceId, _fromSegmentId, Arg.Any<CancellationToken>())
            .Returns(new Segments { Id = _fromSegmentId, RaceId = _raceId, Name = "From" });
    }

    private Task Send(Guid? segmentId) =>
        new MoveAidStationCommandHandler(_segments, _stations).Handle(
            new MoveAidStationCommand(_appUserId, _raceId, _station.Id, new MoveAidStationRequest(segmentId)),
            CancellationToken.None);

    [Fact]
    public async Task Handle_ToEmptySegment_Reassigns()
    {
        await Send(_toSegmentId);

        Assert.Equal(_toSegmentId, _station.SegmentId);
        await _stations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ToOccupiedSegment_SwapsBothStations()
    {
        var occupant = new AidStations { Id = Guid.NewGuid(), SegmentId = _toSegmentId, Name = "Occupant" };
        _stations.GetBySegmentIdAsync(_appUserId, _raceId, _toSegmentId, Arg.Any<CancellationToken>()).Returns(occupant);

        await Send(_toSegmentId);

        Assert.Equal(_toSegmentId, _station.SegmentId);
        Assert.Equal(_fromSegmentId, occupant.SegmentId);
        await _stations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ToItsOwnSegment_DoesNothing()
    {
        await Send(_fromSegmentId);

        Assert.Equal(_fromSegmentId, _station.SegmentId);
        await _stations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownTargetSegment_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Send(Guid.NewGuid()));
    }

    [Fact]
    public async Task Handle_WithoutTarget_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => Send(null));
    }
}
