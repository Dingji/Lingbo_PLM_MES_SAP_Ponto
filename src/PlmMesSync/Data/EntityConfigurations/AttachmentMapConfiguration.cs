using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class AttachmentMapConfiguration : IEntityTypeConfiguration<AttachmentMapEntity>
{
    public void Configure(EntityTypeBuilder<AttachmentMapEntity> builder)
    {
        builder.ToTable("ATTACHMENT_MAP", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.ParentId).HasColumnName("PARENT_ID");
        builder.Property(e => e.AttachId).HasColumnName("ATTACH_ID");
        builder.Property(e => e.Version).HasColumnName("VERSION");
    }
}
