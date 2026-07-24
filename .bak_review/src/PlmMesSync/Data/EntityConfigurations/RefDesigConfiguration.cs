using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class RefDesigConfiguration : IEntityTypeConfiguration<RefDesigEntity>
{
    public void Configure(EntityTypeBuilder<RefDesigEntity> builder)
    {
        builder.ToTable("REFDESIG", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.Bom).HasColumnName("BOM");
        builder.Property(e => e.Label).HasColumnName("LABEL").HasMaxLength(40);
    }
}
