using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicRec.Catalog.Entities;

namespace MusicRec.Catalog.Configurations;

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("Genres");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(g => g.Name)
            .IsUnique();
    }
}
