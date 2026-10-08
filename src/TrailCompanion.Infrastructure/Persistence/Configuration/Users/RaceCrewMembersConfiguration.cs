using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class RaceCrewMembersConfiguration : IEntityTypeConfiguration<RaceCrewMembers>
{
    public void Configure(EntityTypeBuilder<RaceCrewMembers> builder)
    {
        builder.ToTable("RaceCrewMembers");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.RaceId, c.AppUserId })
            .IsUnique();

        builder.HasIndex(c => c.AppUserId);

        builder.HasOne<Races>()
            .WithMany()
            .HasForeignKey(c => c.RaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
