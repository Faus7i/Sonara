using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Favorites.Entities;

namespace MusicRec.Favorites.Configurations;

public class UserLikeConfiguration : IEntityTypeConfiguration<UserLike>
{
    public void Configure(EntityTypeBuilder<UserLike> builder)
    {
        builder.ToTable("UserLikes");
        builder.HasKey(ul => ul.Id);

        builder.Property(ul => ul.UserId).IsRequired();
        builder.Property(ul => ul.TrackId).IsRequired();

        // 业务唯一约束：同一用户不能重复收藏同一曲目（并发安全的最后防线）
        builder.HasIndex(ul => new { ul.UserId, ul.TrackId }).IsUnique();
        // 加速按用户查询收藏列表
        builder.HasIndex(ul => ul.UserId);
    }
}
