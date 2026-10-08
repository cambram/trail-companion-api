namespace TrailCompanion.Domain.Modules.Users.Entities;

public class RaceCrewMembers : BaseEntity
{
    public Guid Id { get; set; }
    public Guid RaceId { get; set; }
    public Guid AppUserId { get; set; }
}
