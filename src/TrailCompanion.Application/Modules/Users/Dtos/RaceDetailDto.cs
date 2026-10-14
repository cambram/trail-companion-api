using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record AidStationDetailDto(
    Guid Id,
    string Name,
    string Notes,
    IReadOnlyList<KitEntryDto> Kit,
    IReadOnlyList<AidStationItemDto> Items)
{
    public static AidStationDetailDto FromEntity(AidStations station, IReadOnlyList<AidStationItemDto> items) => new(
        station.Id,
        station.Name,
        station.Notes,
        station.Kit.Select(KitEntryDto.FromEntity).ToList(),
        items);
}

public record SegmentDetailDto(
    Guid Id,
    int Position,
    string Name,
    int Duration,
    double? EndKm,
    int? AscentM,
    string Notes,
    IReadOnlyList<ItemDto> Items,
    AidStationDetailDto? AidStation)
{
    public static SegmentDetailDto FromEntity(
        Segments segment,
        IReadOnlyList<ItemDto> items,
        AidStationDetailDto? aidStation) => new(
        segment.Id,
        segment.Position,
        segment.Name,
        segment.Duration,
        segment.EndKm,
        segment.AscentM,
        segment.Notes,
        items,
        aidStation);
}

public record RaceDetailDto(
    Guid Id,
    string Name,
    int Target,
    TimeOnly? StartTime,
    DateTime CreatedAtUtc,
    IReadOnlyList<SegmentDetailDto> Segments,
    DateOnly? RaceDate,
    RaceRole Role,
    string? Pin,
    int ReminderIntervalMin,
    IReadOnlyList<ReminderDto> Reminders)
{
    public static RaceDetailDto FromEntity(
        Races race,
        IReadOnlyList<SegmentDetailDto> segments,
        IReadOnlyList<ReminderDto> reminders,
        RaceRole role) => new(
        race.Id,
        race.Name,
        race.Target,
        race.StartTime,
        race.CreatedAtUtc,
        segments,
        race.RaceDate,
        role,
        role == RaceRole.Owner ? race.Pin : null,
        race.ReminderIntervalMin,
        reminders);
}
