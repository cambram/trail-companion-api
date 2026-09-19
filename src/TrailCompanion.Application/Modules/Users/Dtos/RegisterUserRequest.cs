namespace TrailCompanion.Application.Modules.Users.Dtos;

public record RegisterUserRequest(
    string? Email,
    string? Password);
