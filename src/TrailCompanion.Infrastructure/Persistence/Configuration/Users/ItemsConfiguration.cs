using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class ItemsConfiguration : IEntityTypeConfiguration<Items>
{
    public void Configure(EntityTypeBuilder<Items> builder)
    {
        builder.ToTable("Items", table => table.HasCheckConstraint("CK_Items_Carbs", "\"Carbs\" >= 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne<Segments>()
            .WithMany()
            .HasForeignKey(i => i.SegmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
