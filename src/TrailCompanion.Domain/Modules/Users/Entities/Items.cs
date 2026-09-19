namespace TrailCompanion.Domain.Modules.Users.Entities;

public class Items : BaseEntity
{
    public Guid Id { get; set; }
    public Guid SegmentId { get; set; }
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Carbs { get; set; }
}
