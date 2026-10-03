namespace TrailCompanion.Domain.Modules.Users.Entities;

public class AidStations : BaseEntity
{
    public Guid Id { get; set; }
    public Guid SegmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<KitEntry> Kit { get; set; } = [];
}
