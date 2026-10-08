using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class RaceRunsConfiguration : IEntityTypeConfiguration<RaceRuns>
{
    public void Configure(EntityTypeBuilder<RaceRuns> builder)
    {
        builder.ToTable("RaceRuns");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.RaceId)
            .IsUnique();

        builder.HasOne<Races>()
            .WithMany()
            .HasForeignKey(r => r.RaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(r => r.StartedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
