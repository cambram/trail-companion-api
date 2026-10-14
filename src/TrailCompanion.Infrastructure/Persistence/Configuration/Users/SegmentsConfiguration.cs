using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class SegmentsConfiguration : IEntityTypeConfiguration<Segments>
{
    public void Configure(EntityTypeBuilder<Segments> builder)
    {
        builder.ToTable("Segments", table =>
        {
            table.HasCheckConstraint("CK_Segments_Duration", "\"Duration\" >= 0");
            table.HasCheckConstraint("CK_Segments_EndKm", "\"EndKm\" IS NULL OR \"EndKm\" >= 0");
            table.HasCheckConstraint("CK_Segments_AscentM", "\"AscentM\" IS NULL OR (\"AscentM\" >= 0 AND \"AscentM\" <= 10000)");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Duration)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.Notes)
            .IsRequired()
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        builder.HasOne<Races>()
            .WithMany()
            .HasForeignKey(s => s.RaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
