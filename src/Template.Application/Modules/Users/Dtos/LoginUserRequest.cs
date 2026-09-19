namespace Template.Application.Modules.Users.Dtos;

public record LoginUserRequest(
    string? Email,
    string? Password);
