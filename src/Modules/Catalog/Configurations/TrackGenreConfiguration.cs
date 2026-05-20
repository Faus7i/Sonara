using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Catalog.Entities;

namespace MusicRec.Catalog.Configurations;

public class TrackGenreConfiguration : IEntityTypeConfiguration<TrackGenre>
{
    public void Configure(EntityTypeBuilder<TrackGenre> builder)
    {
        builder.ToTable("TrackGenres");

        builder.HasKey(tg => new { tg.TrackId, tg.GenreId });

        builder.HasOne(tg => tg.Track)
            .WithMany(t => t.TrackGenres)
            .HasForeignKey(tg => tg.TrackId);

        builder.HasOne(tg => tg.Genre)
            .WithMany(g => g.TrackGenres)
            .HasForeignKey(tg => tg.GenreId);
    }
}
