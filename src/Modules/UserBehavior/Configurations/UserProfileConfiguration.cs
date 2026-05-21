using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.FavoriteGenres).HasMaxLength(500);
        builder.Property(p => p.AvgEnergy).IsRequired();
        builder.Property(p => p.AvgDanceability).IsRequired();
        builder.Property(p => p.AvgValence).IsRequired();
        builder.Property(p => p.AvgTempo).IsRequired();
        builder.Property(p => p.AvgAcousticness).IsRequired();
        builder.Property(p => p.TopArtists).HasMaxLength(2000);
        builder.Property(p => p.TopTracks).HasMaxLength(2000);
        builder.Property(p => p.ExplorationLevel).IsRequired();
        builder.Property(p => p.TotalPlayCount).IsRequired();
        builder.Property(p => p.LastUpdatedAt).IsRequired();
    }
}
