using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class ListEntryConfiguration : IEntityTypeConfiguration<ListEntryEntity>
{
    public void Configure(EntityTypeBuilder<ListEntryEntity> builder)
    {
        builder.ToTable("LISTENTRY", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.ParentId).HasColumnName("PARENTID");
        builder.Property(e => e.Active).HasColumnName("ACTIVE");
        builder.Property(e => e.EntryId).HasColumnName("ENTRYID");
        builder.Property(e => e.EntryValue).HasColumnName("ENTRYVALUE").HasMaxLength(2048);
        builder.Property(e => e.LangId).HasColumnName("LANGID");
        builder.Property(e => e.Description).HasColumnName("DESCRIPTION").HasMaxLength(2048);
    }
}
