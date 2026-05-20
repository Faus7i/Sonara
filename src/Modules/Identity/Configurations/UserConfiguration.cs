using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Identity.Entities;

namespace MusicRec.Identity.Configurations;

/// <summary>
/// User 实体 EF Core 映射配置
/// </summary>
/// <remarks>
/// Email 唯一索引确保数据库层面的并发安全（配合应用层的 ConflictException）。
/// PasswordHash 使用 nvarchar(max) 以适应 PBKDF2 输出的可变长度。
/// </remarks>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        // 唯一索引：防止并发注册同一邮箱
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Nickname)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);
    }
}
