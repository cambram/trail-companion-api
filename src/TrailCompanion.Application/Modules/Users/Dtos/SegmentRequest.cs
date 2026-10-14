namespace TrailCompanion.Application.Modules.Users.Dtos;

public record SegmentRequest(string? Name, int? Duration, double? EndKm, string? Notes, int? AscentM = null);
