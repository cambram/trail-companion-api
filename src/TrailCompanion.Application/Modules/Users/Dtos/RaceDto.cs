using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record RaceDto(
    Guid Id,
    string Name,
    int Target,
    TimeOnly? StartTime,
    DateTime CreatedAtUtc)
{
    public static RaceDto FromEntity(Races race) => new(
        race.Id,
        race.Name,
        race.Target,
        race.StartTime,
        race.CreatedAtUtc);
}
