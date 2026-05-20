using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Catalog.Entities;

namespace MusicRec.Catalog.Configurations;

public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("Artists");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SpotifyArtistId)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(a => a.SpotifyArtistId)
            .IsUnique();

        builder.Property(a => a.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.ImageUrl)
            .HasMaxLength(500);

        builder.Property(a => a.Genres)
            .HasMaxLength(2000);
    }
}
