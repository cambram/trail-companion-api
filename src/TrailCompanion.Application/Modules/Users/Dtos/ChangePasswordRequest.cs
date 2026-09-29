namespace TrailCompanion.Application.Modules.Users.Dtos;

public record ChangePasswordRequest(
    string? CurrentPassword,
    string? NewPassword);
