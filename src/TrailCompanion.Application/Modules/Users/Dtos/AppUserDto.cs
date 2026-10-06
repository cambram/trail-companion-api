using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record AppUserDto(
    Guid Id,
    string Username,
    string? FirstName,
    string? LastName,
    DateTime CreatedAtUtc)
{
    public static AppUserDto FromEntity(AppUser appUser) => new(
        appUser.Id,
        appUser.Username,
        appUser.FirstName,
        appUser.LastName,
        appUser.CreatedAtUtc);
}
