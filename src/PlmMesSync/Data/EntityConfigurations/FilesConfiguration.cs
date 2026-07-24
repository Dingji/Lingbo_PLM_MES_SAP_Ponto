using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class FilesConfiguration : IEntityTypeConfiguration<FilesEntity>
{
    public void Configure(EntityTypeBuilder<FilesEntity> builder)
    {
        builder.ToTable("FILES", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.FileName).HasColumnName("FILENAME").HasMaxLength(4000);
    }
}
