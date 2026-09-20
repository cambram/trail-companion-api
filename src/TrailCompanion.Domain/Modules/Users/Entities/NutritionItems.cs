namespace TrailCompanion.Domain.Modules.Users.Entities;

public class NutritionItems : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AppUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public NutritionType Type { get; set; }
    public int? Size { get; set; }
    public int? Carbs { get; set; }
    public int? Calories { get; set; }
    public int? Caffeine { get; set; }
    public decimal? Sodium { get; set; }
    public decimal? Potassium { get; set; }
    public decimal? Magnesium { get; set; }
    public decimal? Calcium { get; set; }
}
