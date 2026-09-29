namespace TrailCompanion.Application.Modules.Users.Dtos;

public record CreateRaceRequest(
    string? Name,
    int? Target,
    TimeOnly? StartTime = null,
    DateOnly? RaceDate = null);
