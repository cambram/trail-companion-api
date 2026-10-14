namespace TrailCompanion.Domain.Modules.Users.Entities;

public class ReminderItems : BaseEntity
{
    public Guid Id { get; set; }
    public Guid RaceId { get; set; }
    public int ReminderNumber { get; set; }
    public Guid NutritionItemId { get; set; }
    public int Position { get; set; }
}
