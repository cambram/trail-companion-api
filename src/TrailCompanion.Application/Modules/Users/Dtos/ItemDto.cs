using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record ItemDto(
    Guid Id,
    Guid SegmentId,
    int Position,
    string Name,
    int Carbs)
{
    public static ItemDto FromEntity(Items item) => new(
        item.Id,
        item.SegmentId,
        item.Position,
        item.Name,
        item.Carbs);
}
