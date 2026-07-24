using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class BomConfiguration : IEntityTypeConfiguration<BomEntity>
{
    public void Configure(EntityTypeBuilder<BomEntity> builder)
    {
        builder.ToTable("BOM", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.Item).HasColumnName("ITEM");
        builder.Property(e => e.ItemNumber).HasColumnName("ITEM_NUMBER").HasMaxLength(300);
        builder.Property(e => e.FindNumber).HasColumnName("FIND_NUMBER").HasMaxLength(32);
        builder.Property(e => e.Quantity).HasColumnName("QUANTITY").HasMaxLength(40);
        builder.Property(e => e.Description).HasColumnName("DESCRIPTION").HasMaxLength(4000);
        builder.Property(e => e.ChangeIn).HasColumnName("CHANGE_IN");
        builder.Property(e => e.ChangeOut).HasColumnName("CHANGE_OUT");
        builder.Property(e => e.Component).HasColumnName("COMPONENT");
        builder.Property(e => e.List06).HasColumnName("LIST06");
        builder.Property(e => e.List07).HasColumnName("LIST07");

        builder.HasOne(e => e.ParentItem)
            .WithMany()
            .HasForeignKey(e => e.Item)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(e => e.RefDesigs)
            .WithOne()
            .HasForeignKey(r => r.Bom)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
