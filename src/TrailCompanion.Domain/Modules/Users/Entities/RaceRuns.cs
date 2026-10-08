namespace TrailCompanion.Domain.Modules.Users.Entities;

public class RaceRuns : BaseEntity
{
    public Guid Id { get; set; }
    public Guid RaceId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public Guid? StartedBy { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}
