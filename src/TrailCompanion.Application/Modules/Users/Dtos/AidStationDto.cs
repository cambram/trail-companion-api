using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record AidStationDto(
    Guid Id,
    Guid SegmentId,
    string Name,
    string Notes,
    IReadOnlyList<KitEntryDto> Kit)
{
    public static AidStationDto FromEntity(AidStations station) => new(
        station.Id,
        station.SegmentId,
        station.Name,
        station.Notes,
        station.Kit.Select(KitEntryDto.FromEntity).ToList());
}
