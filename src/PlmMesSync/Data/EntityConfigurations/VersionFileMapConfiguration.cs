using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class VersionFileMapConfiguration : IEntityTypeConfiguration<VersionFileMapEntity>
{
    public void Configure(EntityTypeBuilder<VersionFileMapEntity> builder)
    {
        builder.ToTable("VERSION_FILE_MAP", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.VersionId).HasColumnName("VERSION_ID");
        builder.Property(e => e.FileId).HasColumnName("FILE_ID");
    }
}
