namespace TrailCompanion.Domain.Modules.Users.Entities;

public class Segments : BaseEntity
{
    public Guid Id { get; set; }
    public Guid RaceId { get; set; }
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; }
    public double? EndKm { get; set; }
    public int? AscentM { get; set; }
    public string Notes { get; set; } = string.Empty;
}
