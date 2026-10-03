using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record CreateAidStationRequest(Guid? SegmentId, string? Name);

public record AidStationRequest(string? Name, string? Notes, IReadOnlyList<KitEntryRequest>? Kit);

public record KitEntryRequest(string? Label, KitCertainty? Certainty);

public record MoveAidStationRequest(Guid? SegmentId);
