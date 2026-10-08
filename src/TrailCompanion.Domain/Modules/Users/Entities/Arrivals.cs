namespace TrailCompanion.Domain.Modules.Users.Entities;

public class Arrivals : BaseEntity
{
    public Guid Id { get; set; }
    public Guid RaceRunId { get; set; }
    public Guid? AidStationId { get; set; }
    public DateTime ArrivedAtUtc { get; set; }
    public Guid? RecordedBy { get; set; }
}
