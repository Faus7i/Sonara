using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Playlists.Entities;

namespace MusicRec.Playlists.Configurations;

public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("Playlists");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.CoverImageUrl).HasMaxLength(500);
        builder.Property(p => p.IsPublic).IsRequired();

        // 加速按用户查询歌单列表
        builder.HasIndex(p => p.UserId);

        // Cascade：删除歌单时自动删除所有关联的 PlaylistTrack
        builder.HasMany(p => p.PlaylistTracks)
            .WithOne(pt => pt.Playlist)
            .HasForeignKey(pt => pt.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
