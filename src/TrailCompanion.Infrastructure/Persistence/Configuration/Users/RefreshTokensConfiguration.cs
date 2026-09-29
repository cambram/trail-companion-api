using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrailCompanion.Infrastructure.Persistence.Configuration.Users;

public class RefreshTokensConfiguration : IEntityTypeConfiguration<RefreshTokens>
{
    public void Configure(EntityTypeBuilder<RefreshTokens> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => t.AppUserId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(t => t.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
