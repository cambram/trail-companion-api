using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record ArrivalDto(string Checkpoint, DateTime ArrivedAtUtc)
{
    public static ArrivalDto FromEntity(Arrivals arrival) => new(RunCheckpoints.KeyOf(arrival), arrival.ArrivedAtUtc);
}

public record RaceRunDto(
    Guid Id,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<ArrivalDto> Arrivals);

public record RaceRunStateDto(
    string TimeZone,
    DateOnly Today,
    bool CanStart,
    RaceRunDto? Run);
