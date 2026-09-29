namespace TrailCompanion.Domain.Modules.Users.Entities;

public class RefreshTokens : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AppUserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
}
