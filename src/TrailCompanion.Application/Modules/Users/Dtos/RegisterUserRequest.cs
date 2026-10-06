namespace TrailCompanion.Application.Modules.Users.Dtos;

public record RegisterUserRequest(
    string? Username,
    string? Password);
