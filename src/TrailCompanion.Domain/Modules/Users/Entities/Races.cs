namespace TrailCompanion.Domain.Modules.Users.Entities;

public class Races : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AppUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Target { get; set; } = 60;
    public int ReminderIntervalMin { get; set; } = 30;
    public TimeOnly? StartTime { get; set; }
    public DateOnly? RaceDate { get; set; }
    public string Pin { get; set; } = string.Empty;
}
