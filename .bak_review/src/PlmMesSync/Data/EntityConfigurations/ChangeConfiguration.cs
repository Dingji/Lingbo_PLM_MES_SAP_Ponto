using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class ChangeConfiguration : IEntityTypeConfiguration<ChangeEntity>
{
    public void Configure(EntityTypeBuilder<ChangeEntity> builder)
    {
        builder.ToTable("CHANGE", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.ChangeNumber).HasColumnName("CHANGE_NUMBER").HasMaxLength(90);
        builder.Property(e => e.Description).HasColumnName("DESCRIPTION").HasMaxLength(4000);
        builder.Property(e => e.Status).HasColumnName("STATUS");
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE");
        builder.Property(e => e.ReleaseDate).HasColumnName("RELEASE_DATE");
    }
}
