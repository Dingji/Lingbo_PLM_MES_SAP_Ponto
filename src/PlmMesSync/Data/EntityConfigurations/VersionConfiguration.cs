using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class VersionConfiguration : IEntityTypeConfiguration<VersionEntity>
{
    public void Configure(EntityTypeBuilder<VersionEntity> builder)
    {
        builder.ToTable("VERSION", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.AttachId).HasColumnName("ATTACH_ID");
        builder.Property(e => e.VersionNum).HasColumnName("VERSION_NUM");
    }
}
