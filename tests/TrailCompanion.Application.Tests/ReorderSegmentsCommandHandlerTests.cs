using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class ReorderSegmentsCommandHandlerTests
{
    private readonly ISegmentsRepository _segments = Substitute.For<ISegmentsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();

    private List<Segments> GivenSegments(int count)
    {
        var list = Enumerable.Range(1, count)
            .Select(p => new Segments { Id = Guid.NewGuid(), RaceId = _raceId, Position = p, Name = $"S{p}" })
            .ToList();
        foreach (var s in list)
        {
            _segments.GetByIdAsync(_appUserId, _raceId, s.Id, Arg.Any<CancellationToken>()).Returns(s);
        }

        _segments.GetByRaceIdAsync(_appUserId, _raceId, Arg.Any<CancellationToken>()).Returns(list);
        return list;
    }

    private Task Reorder(params Guid[] ids) => new ReorderSegmentsCommandHandler(_segments).Handle(
        new ReorderSegmentsCommand(_appUserId, _raceId, new ReorderRequest(ids)), CancellationToken.None);

    [Fact]
    public async Task Handle_AssignsPositionsInListOrder()
    {
        var s = GivenSegments(3);

        await Reorder(s[2].Id, s[0].Id, s[1].Id);

        Assert.Equal([2, 3, 1], s.Select(x => x.Position));
        await _segments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithIncompleteOrDuplicateIds_ThrowsValidationFailed()
    {
        var s = GivenSegments(2);

        await Assert.ThrowsAsync<ValidationFailedException>(() => Reorder(s[0].Id));
        await Assert.ThrowsAsync<ValidationFailedException>(() => Reorder(s[0].Id, s[0].Id));
        await _segments.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownId_ThrowsNotFound()
    {
        GivenSegments(1);

        await Assert.ThrowsAsync<NotFoundException>(() => Reorder(Guid.NewGuid()));
    }
}
