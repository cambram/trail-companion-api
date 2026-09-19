using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class DeleteSegmentCommandHandlerTests
{
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();

    private DeleteSegmentCommandHandler CreateHandler() => new(_segments);

    [Fact]
    public async Task Handle_WhenSegmentExists_DeletesAndSaves()
    {
        var segment = new Segments { Id = Guid.NewGuid(), RaceId = _raceId, Name = "A" };
        _segments.GetByIdAsync(_appUserId, _raceId, segment.Id, Arg.Any<CancellationToken>()).Returns(segment);

        await CreateHandler().Handle(new DeleteSegmentCommand(_appUserId, _raceId, segment.Id), CancellationToken.None);

        _segments.Received(1).Delete(segment);
        await _segments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSegmentMissingOrNotOwned_ThrowsNotFoundAndDeletesNothing()
    {
        var segmentId = Guid.NewGuid();
        _segments.GetByIdAsync(_appUserId, _raceId, segmentId, Arg.Any<CancellationToken>()).Returns((Segments?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteSegmentCommand(_appUserId, _raceId, segmentId), CancellationToken.None));

        _segments.DidNotReceive().Delete(Arg.Any<Segments>());
        await _segments.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
