using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class AidStationsConfiguration : IEntityTypeConfiguration<AidStations>
{
    public void Configure(EntityTypeBuilder<AidStations> builder)
    {
        builder.ToTable("AidStations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Notes)
            .IsRequired()
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        builder.ComplexCollection(a => a.Kit, kit =>
        {
            kit.ToJson();
            kit.Property(e => e.Certainty).HasConversion<string>();
        });

        builder.HasOne<Segments>()
            .WithMany()
            .HasForeignKey(a => a.SegmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
