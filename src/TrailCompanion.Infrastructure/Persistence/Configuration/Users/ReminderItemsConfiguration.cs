using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class ReminderItemsConfiguration : IEntityTypeConfiguration<ReminderItems>
{
    public void Configure(EntityTypeBuilder<ReminderItems> builder)
    {
        builder.ToTable("ReminderItems", table =>
        {
            table.HasCheckConstraint("CK_ReminderItems_ReminderNumber", "\"ReminderNumber\" BETWEEN 1 AND 500");
        });

        builder.HasKey(i => i.Id);

        builder.HasIndex(i => new { i.RaceId, i.ReminderNumber, i.Position });

        builder.HasOne<Races>()
            .WithMany()
            .HasForeignKey(i => i.RaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NutritionItems>()
            .WithMany()
            .HasForeignKey(i => i.NutritionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
