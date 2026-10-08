namespace TrailCompanion.Application.Modules.Users.Dtos;

public record StartRunRequest(DateTimeOffset? StartedAt);

public record EditRunRequest(DateTimeOffset? StartedAt);

public record RecordArrivalRequest(string? Checkpoint, DateTimeOffset? ArrivedAt);

public record EditArrivalRequest(DateTimeOffset? ArrivedAt);
