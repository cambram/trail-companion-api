using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class AidStationItemsConfiguration : IEntityTypeConfiguration<AidStationItems>
{
    public void Configure(EntityTypeBuilder<AidStationItems> builder)
    {
        builder.ToTable("AidStationItems");

        builder.HasKey(i => i.Id);

        builder.HasOne<AidStations>()
            .WithMany()
            .HasForeignKey(i => i.AidStationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NutritionItems>()
            .WithMany()
            .HasForeignKey(i => i.NutritionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
