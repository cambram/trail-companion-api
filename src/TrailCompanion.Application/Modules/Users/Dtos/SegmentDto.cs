using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record SegmentDto(
    Guid Id,
    Guid RaceId,
    int Position,
    string Name,
    int Duration,
    double? EndKm,
    string Notes)
{
    public static SegmentDto FromEntity(Segments segment) => new(
        segment.Id,
        segment.RaceId,
        segment.Position,
        segment.Name,
        segment.Duration,
        segment.EndKm,
        segment.Notes);
}
