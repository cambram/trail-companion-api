using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class NutritionItemsConfiguration : IEntityTypeConfiguration<NutritionItems>
{
    public void Configure(EntityTypeBuilder<NutritionItems> builder)
    {
        builder.ToTable("NutritionItems", table =>
        {
            table.HasCheckConstraint("CK_NutritionItems_Size", "\"Size\" IS NULL OR \"Size\" >= 0");
            table.HasCheckConstraint("CK_NutritionItems_Carbs", "\"Carbs\" IS NULL OR \"Carbs\" >= 0");
            table.HasCheckConstraint("CK_NutritionItems_Calories", "\"Calories\" IS NULL OR \"Calories\" >= 0");
            table.HasCheckConstraint("CK_NutritionItems_Caffeine", "\"Caffeine\" IS NULL OR \"Caffeine\" >= 0");
            table.HasCheckConstraint("CK_NutritionItems_Sodium", "\"Sodium\" IS NULL OR \"Sodium\" >= 0");
            table.HasCheckConstraint("CK_NutritionItems_Potassium", "\"Potassium\" IS NULL OR \"Potassium\" >= 0");
            table.HasCheckConstraint("CK_NutritionItems_Magnesium", "\"Magnesium\" IS NULL OR \"Magnesium\" >= 0");
            table.HasCheckConstraint("CK_NutritionItems_Calcium", "\"Calcium\" IS NULL OR \"Calcium\" >= 0");
        });

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.Sodium).HasPrecision(8, 2);
        builder.Property(n => n.Potassium).HasPrecision(8, 2);
        builder.Property(n => n.Magnesium).HasPrecision(8, 2);
        builder.Property(n => n.Calcium).HasPrecision(8, 2);

        builder.HasIndex(n => new { n.AppUserId, n.Name })
            .IsUnique();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(n => n.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
