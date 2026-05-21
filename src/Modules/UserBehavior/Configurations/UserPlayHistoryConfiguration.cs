using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Configurations;

public class UserPlayHistoryConfiguration : IEntityTypeConfiguration<UserPlayHistory>
{
    public void Configure(EntityTypeBuilder<UserPlayHistory> builder)
    {
        builder.ToTable("UserPlayHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.UserId).IsRequired();
        builder.Property(h => h.TrackId).IsRequired();
        builder.Property(h => h.PlayedAt).IsRequired();
        builder.Property(h => h.DurationPlayed).IsRequired();
        builder.Property(h => h.Completed).IsRequired();
        builder.Property(h => h.Source).HasMaxLength(50);

        builder.HasIndex(h => new { h.UserId, h.PlayedAt }).IsDescending(false, true);
        builder.HasIndex(h => new { h.UserId, h.TrackId, h.PlayedAt });
    }
}
