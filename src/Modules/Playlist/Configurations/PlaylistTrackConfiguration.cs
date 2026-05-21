using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Playlists.Entities;

namespace MusicRec.Playlists.Configurations;

public class PlaylistTrackConfiguration : IEntityTypeConfiguration<PlaylistTrack>
{
    public void Configure(EntityTypeBuilder<PlaylistTrack> builder)
    {
        builder.ToTable("PlaylistTracks");
        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.PlaylistId).IsRequired();
        builder.Property(pt => pt.TrackId).IsRequired();
        builder.Property(pt => pt.OrderIndex).IsRequired();

        // 业务唯一约束：同一歌单中不能有重复曲目（并发安全的最后防线）
        builder.HasIndex(pt => new { pt.PlaylistId, pt.TrackId }).IsUnique();
        // 加速按歌单 + 排序查询（GetPlaylistDetailQueryHandler 的核心查询路径）
        builder.HasIndex(pt => new { pt.PlaylistId, pt.OrderIndex });
    }
}
