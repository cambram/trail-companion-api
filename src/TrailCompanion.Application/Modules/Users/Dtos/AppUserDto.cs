using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record AppUserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    DateTime CreatedAtUtc)
{
    public static AppUserDto FromEntity(AppUser appUser) => new(
        appUser.Id,
        appUser.Email,
        appUser.FirstName,
        appUser.LastName,
        appUser.CreatedAtUtc);
}
