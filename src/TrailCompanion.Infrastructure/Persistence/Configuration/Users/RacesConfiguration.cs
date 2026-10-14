using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class RacesConfiguration : IEntityTypeConfiguration<Races>
{
    public void Configure(EntityTypeBuilder<Races> builder)
    {
        builder.ToTable("Races", table =>
        {
            table.HasCheckConstraint("CK_Races_Target", "\"Target\" >= 0");
            table.HasCheckConstraint("CK_Races_Pin", "\"Pin\" ~ '^[0-9]{6}$'");
            table.HasCheckConstraint("CK_Races_ReminderIntervalMin", "\"ReminderIntervalMin\" BETWEEN 5 AND 180");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Target)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(r => r.ReminderIntervalMin)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(r => r.Pin)
            .IsRequired()
            .HasMaxLength(6);

        builder.HasIndex(r => r.Pin)
            .IsUnique();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(r => r.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
