namespace TrailCompanion.Domain.Modules.Users.Entities;

public class AppUser : BaseEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? PasswordChangedAtUtc { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
