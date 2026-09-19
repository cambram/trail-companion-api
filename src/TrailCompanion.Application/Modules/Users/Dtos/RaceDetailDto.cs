using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record SegmentDetailDto(
    Guid Id,
    int Position,
    string Name,
    int Duration,
    double? Distance,
    string Notes,
    IReadOnlyList<ItemDto> Items)
{
    public static SegmentDetailDto FromEntity(Segments segment, IReadOnlyList<ItemDto> items) => new(
        segment.Id,
        segment.Position,
        segment.Name,
        segment.Duration,
        segment.Distance,
        segment.Notes,
        items);
}

public record RaceDetailDto(
    Guid Id,
    string Name,
    int Target,
    TimeOnly? StartTime,
    DateTime CreatedAtUtc,
    IReadOnlyList<SegmentDetailDto> Segments)
{
    public static RaceDetailDto FromEntity(Races race, IReadOnlyList<SegmentDetailDto> segments) => new(
        race.Id,
        race.Name,
        race.Target,
        race.StartTime,
        race.CreatedAtUtc,
        segments);
}
