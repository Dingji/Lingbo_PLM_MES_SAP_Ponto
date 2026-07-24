using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data.EntityConfigurations;

public class FileInfoConfiguration : IEntityTypeConfiguration<FileInfoEntity>
{
    public void Configure(EntityTypeBuilder<FileInfoEntity> builder)
    {
        builder.ToTable("FILE_INFO", "AGILE");
        builder.HasKey(e => e.FileId);
        builder.Property(e => e.FileId).HasColumnName("FILE_ID");
        builder.Property(e => e.IfsFilepath).HasColumnName("IFS_FILEPATH").HasMaxLength(4000);
    }
}
