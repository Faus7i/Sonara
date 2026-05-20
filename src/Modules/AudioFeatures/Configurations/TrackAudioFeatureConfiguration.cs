using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.AudioFeatures.Entities;

namespace MusicRec.AudioFeatures.Configurations;

public class TrackAudioFeatureConfiguration : IEntityTypeConfiguration<TrackAudioFeature>
{
    public void Configure(EntityTypeBuilder<TrackAudioFeature> builder)
    {
        builder.ToTable("TrackAudioFeatures");

        builder.HasKey(af => af.Id);

        builder.Property(af => af.SpotifyTrackId)
            .HasMaxLength(64)
            .IsRequired();

        // 一对一：一个 TrackId 只能有一份音频特征
        builder.HasIndex(af => af.SpotifyTrackId)
            .IsUnique();
    }
}
