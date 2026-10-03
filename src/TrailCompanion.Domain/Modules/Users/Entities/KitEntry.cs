namespace TrailCompanion.Domain.Modules.Users.Entities;

public class KitEntry
{
    public string Label { get; set; } = string.Empty;
    public KitCertainty Certainty { get; set; }
}
