using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.UserBehavior.Entities;

namespace MusicRec.UserBehavior.Configurations;

public class UserBehaviorEventConfiguration : IEntityTypeConfiguration<UserBehaviorEvent>
{
    public void Configure(EntityTypeBuilder<UserBehaviorEvent> builder)
    {
        builder.ToTable("UserBehaviorEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Context).HasMaxLength(100);
        builder.Property(e => e.Metadata).HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.EventType, e.CreatedAt }).IsDescending(false, false, true);
        builder.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
    }
}
