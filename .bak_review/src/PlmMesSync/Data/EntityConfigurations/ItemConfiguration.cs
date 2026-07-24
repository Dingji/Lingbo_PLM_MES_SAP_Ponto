using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class ItemConfiguration : IEntityTypeConfiguration<ItemEntity>
{
    public void Configure(EntityTypeBuilder<ItemEntity> builder)
    {
        builder.ToTable("ITEM", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.ItemNumber).HasColumnName("ITEM_NUMBER").HasMaxLength(300);
        builder.Property(e => e.Description).HasColumnName("DESCRIPTION").HasMaxLength(720);
        builder.Property(e => e.DefaultChange).HasColumnName("DEFAULT_CHANGE");
    }
}
