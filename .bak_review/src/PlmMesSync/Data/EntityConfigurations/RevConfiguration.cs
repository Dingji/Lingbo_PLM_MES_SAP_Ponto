using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class RevConfiguration : IEntityTypeConfiguration<RevEntity>
{
    public void Configure(EntityTypeBuilder<RevEntity> builder)
    {
        builder.ToTable("REV", "AGILE");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("ID");
        builder.Property(e => e.Item).HasColumnName("ITEM");
        builder.Property(e => e.Change).HasColumnName("CHANGE");
        builder.Property(e => e.RevNumber).HasColumnName("REV_NUMBER").HasMaxLength(40);
        builder.Property(e => e.OldRevNumber).HasColumnName("OLD_REVNUMBER").HasMaxLength(40);
        builder.Property(e => e.ReleaseDate).HasColumnName("RELEASE_DATE");
        builder.Property(e => e.LatestFlag).HasColumnName("LATEST_FLAG");
        builder.Property(e => e.Description).HasColumnName("DESCRIPTION").HasMaxLength(720);
    }
}
