using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Search.Entities;

namespace MusicRec.Search.Configurations;

public class SearchHistoryConfiguration : IEntityTypeConfiguration<SearchHistory>
{
    public void Configure(EntityTypeBuilder<SearchHistory> builder)
    {
        builder.ToTable("UserSearchHistory");

        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.Keyword)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(sh => sh.UserId);
        builder.HasIndex(sh => sh.SearchedAt);
    }
}
