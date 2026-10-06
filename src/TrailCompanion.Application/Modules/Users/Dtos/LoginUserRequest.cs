namespace TrailCompanion.Application.Modules.Users.Dtos;

public record LoginUserRequest(
    string? Username,
    string? Password);
