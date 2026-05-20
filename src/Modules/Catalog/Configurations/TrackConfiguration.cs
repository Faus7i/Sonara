using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Catalog.Entities;

namespace MusicRec.Catalog.Configurations;

public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("Tracks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SpotifyTrackId)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(t => t.SpotifyTrackId)
            .IsUnique();

        builder.Property(t => t.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.ReleaseDate)
            .HasMaxLength(32);

        builder.Property(t => t.CoverImageUrl)
            .HasMaxLength(500);

        builder.HasOne(t => t.Album)
            .WithMany(a => a.Tracks)
            .HasForeignKey(t => t.AlbumId);
    }
}
