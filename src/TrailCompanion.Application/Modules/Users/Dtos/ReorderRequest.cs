namespace TrailCompanion.Application.Modules.Users.Dtos;

public record ReorderRequest(IReadOnlyList<Guid>? Ids);
