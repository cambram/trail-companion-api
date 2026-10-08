using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record RaceDto(
    Guid Id,
    string Name,
    int Target,
    TimeOnly? StartTime,
    DateTime CreatedAtUtc,
    DateOnly? RaceDate,
    RaceRole Role)
{
    public static RaceDto FromEntity(Races race, RaceRole role = RaceRole.Owner) => new(
        race.Id,
        race.Name,
        race.Target,
        race.StartTime,
        race.CreatedAtUtc,
        race.RaceDate,
        role);
}
