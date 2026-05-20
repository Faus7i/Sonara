using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Catalog.Entities;

namespace MusicRec.Catalog.Configurations;

public class TrackArtistConfiguration : IEntityTypeConfiguration<TrackArtist>
{
    public void Configure(EntityTypeBuilder<TrackArtist> builder)
    {
        builder.ToTable("TrackArtists");

        // 联合主键
        builder.HasKey(ta => new { ta.TrackId, ta.ArtistId });

        builder.HasOne(ta => ta.Track)
            .WithMany(t => t.TrackArtists)
            .HasForeignKey(ta => ta.TrackId);

        builder.HasOne(ta => ta.Artist)
            .WithMany(a => a.TrackArtists)
            .HasForeignKey(ta => ta.ArtistId);
    }
}
