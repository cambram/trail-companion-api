using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record KitEntryDto(string Label, KitCertainty Certainty)
{
    public static KitEntryDto FromEntity(KitEntry entry) => new(entry.Label, entry.Certainty);
}
