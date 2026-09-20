using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class ItemsConfiguration : IEntityTypeConfiguration<Items>
{
    public void Configure(EntityTypeBuilder<Items> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(i => i.Id);

        builder.HasOne<Segments>()
            .WithMany()
            .HasForeignKey(i => i.SegmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NutritionItems>()
            .WithMany()
            .HasForeignKey(i => i.NutritionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
