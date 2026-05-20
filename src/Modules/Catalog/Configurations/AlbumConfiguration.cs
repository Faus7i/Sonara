using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Catalog.Entities;

namespace MusicRec.Catalog.Configurations;

public class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("Albums");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SpotifyAlbumId)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(a => a.SpotifyAlbumId)
            .IsUnique();

        builder.Property(a => a.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.ReleaseDate)
            .HasMaxLength(32);

        builder.Property(a => a.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(a => a.AlbumType)
            .HasMaxLength(32);
    }
}
