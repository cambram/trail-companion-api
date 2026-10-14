using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class UpdateRaceReminderIntervalTests
{
    private readonly IRacesRepository _races = Substitute.For<IRacesRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Races _race;

    public UpdateRaceReminderIntervalTests()
    {
        _race = new Races { Id = Guid.NewGuid(), AppUserId = _appUserId, Name = "UTCT", Target = 60, ReminderIntervalMin = 30 };
        _races.GetByIdAsync(_appUserId, _race.Id, Arg.Any<CancellationToken>()).Returns(_race);
    }

    private Task<RaceDto> Send(int? interval) =>
        new UpdateRaceCommandHandler(_races).Handle(
            new UpdateRaceCommand(_appUserId, _race.Id, new CreateRaceRequest("UTCT", null, null, null, interval)),
            CancellationToken.None);

    [Theory]
    [InlineData(5)]
    [InlineData(45)]
    [InlineData(180)]
    public async Task Handle_WithAnIntervalInRange_StoresIt(int interval)
    {
        var result = await Send(interval);

        Assert.Equal(interval, result.ReminderIntervalMin);
        Assert.Equal(interval, _race.ReminderIntervalMin);
        await _races.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutAnInterval_KeepsTheCurrentOne()
    {
        _race.ReminderIntervalMin = 45;

        var result = await Send(null);

        Assert.Equal(45, result.ReminderIntervalMin);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(181)]
    [InlineData(-30)]
    public async Task Handle_WithAnIntervalOutOfRange_ThrowsValidationAndDoesNotSave(int interval)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Send(interval));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidReminderInterval");
        Assert.Equal(30, _race.ReminderIntervalMin);
        await _races.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
