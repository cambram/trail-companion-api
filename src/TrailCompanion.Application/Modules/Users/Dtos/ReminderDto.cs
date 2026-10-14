namespace TrailCompanion.Application.Modules.Users.Dtos;

public record ReminderDto(int Number, IReadOnlyList<ReminderItemDto> Items);
