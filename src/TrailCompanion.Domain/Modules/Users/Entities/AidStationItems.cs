namespace TrailCompanion.Domain.Modules.Users.Entities;

public class AidStationItems : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AidStationId { get; set; }
    public Guid NutritionItemId { get; set; }
    public int Position { get; set; }
}
