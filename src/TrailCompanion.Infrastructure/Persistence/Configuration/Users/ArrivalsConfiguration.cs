using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class ArrivalsConfiguration : IEntityTypeConfiguration<Arrivals>
{
    public void Configure(EntityTypeBuilder<Arrivals> builder)
    {
        builder.ToTable("Arrivals");

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.RaceRunId, a.AidStationId })
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasOne<RaceRuns>()
            .WithMany()
            .HasForeignKey(a => a.RaceRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AidStations>()
            .WithMany()
            .HasForeignKey(a => a.AidStationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(a => a.RecordedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
