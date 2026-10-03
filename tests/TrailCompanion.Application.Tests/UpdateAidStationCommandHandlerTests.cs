using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class UpdateAidStationCommandHandlerTests
{
    private readonly IAidStationsRepository _stations = Substitute.For<IAidStationsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _raceId = Guid.NewGuid();
    private readonly AidStations _station = new() { Id = Guid.NewGuid(), SegmentId = Guid.NewGuid(), Name = "Old" };

    public UpdateAidStationCommandHandlerTests()
    {
        _stations.GetByIdAsync(_appUserId, _raceId, _station.Id, Arg.Any<CancellationToken>()).Returns(_station);
    }

    private Task<AidStationDto> Send(AidStationRequest request) =>
        new UpdateAidStationCommandHandler(_stations)
            .Handle(new UpdateAidStationCommand(_appUserId, _raceId, _station.Id, request), CancellationToken.None);

    [Fact]
    public async Task Handle_ReplacesNameNotesAndKitTrimmed()
    {
        var result = await Send(new AidStationRequest(
            "  Hout Bay ",
            "10 min",
            [new KitEntryRequest(" Socks ", KitCertainty.Planned), new KitEntryRequest("Jacket", KitCertainty.Maybe)]));

        Assert.Equal("Hout Bay", result.Name);
        Assert.Equal("10 min", result.Notes);
        Assert.Equal([new KitEntryDto("Socks", KitCertainty.Planned), new KitEntryDto("Jacket", KitCertainty.Maybe)], result.Kit);
        Assert.Equal("Socks", _station.Kit[0].Label);
        await _stations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullNotesAndKit_StoresEmpty()
    {
        _station.Kit = [new KitEntry { Label = "Old", Certainty = KitCertainty.Planned }];

        var result = await Send(new AidStationRequest("A", null, null));

        Assert.Equal(string.Empty, result.Notes);
        Assert.Empty(result.Kit);
    }

    public static TheoryData<AidStationRequest, string> InvalidRequests => new()
    {
        { new AidStationRequest("   ", null, null), "InvalidName" },
        { new AidStationRequest(new string('n', 201), null, null), "InvalidName" },
        { new AidStationRequest("A", new string('n', 2001), null), "InvalidNotes" },
        { new AidStationRequest("A", null, Enumerable.Repeat(new KitEntryRequest("x", KitCertainty.Planned), 31).ToList()), "InvalidKit" },
        { new AidStationRequest("A", null, [null!]), "InvalidKitLabel" },
        { new AidStationRequest("A", null, [new KitEntryRequest("   ", KitCertainty.Planned)]), "InvalidKitLabel" },
        { new AidStationRequest("A", null, [new KitEntryRequest(new string('k', 101), KitCertainty.Planned)]), "InvalidKitLabel" },
        { new AidStationRequest("A", null, [new KitEntryRequest("Socks", null)]), "InvalidKitCertainty" },
        { new AidStationRequest("A", null, [new KitEntryRequest("Socks", (KitCertainty)7)]), "InvalidKitCertainty" }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Handle_WithInvalidRequest_ThrowsValidationAndDoesNotSave(AidStationRequest request, string code)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Send(request));

        Assert.Contains(exception.Failures, f => f.Code == code);
        await _stations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownStation_ThrowsNotFound()
    {
        var command = new UpdateAidStationCommand(_appUserId, _raceId, Guid.NewGuid(), new AidStationRequest("A", null, null));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateAidStationCommandHandler(_stations).Handle(command, CancellationToken.None));
    }
}
