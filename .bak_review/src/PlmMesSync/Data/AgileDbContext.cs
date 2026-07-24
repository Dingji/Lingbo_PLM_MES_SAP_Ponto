using Microsoft.EntityFrameworkCore;
using PlmMesSync.Data.EntityConfigurations;
using PlmMesSync.Models.Entities;

namespace PlmMesSync.Data;

public class AgileDbContext : DbContext
{
    public AgileDbContext(DbContextOptions<AgileDbContext> options) : base(options) { }

    public DbSet<ItemEntity> Items => Set<ItemEntity>();
    public DbSet<BomEntity> Boms => Set<BomEntity>();
    public DbSet<RefDesigEntity> RefDesigs => Set<RefDesigEntity>();
    public DbSet<RevEntity> Revs => Set<RevEntity>();
    public DbSet<ChangeEntity> Changes => Set<ChangeEntity>();
    public DbSet<ListEntryEntity> ListEntries => Set<ListEntryEntity>();
    public DbSet<FilesEntity> Files => Set<FilesEntity>();
    public DbSet<FileInfoEntity> FileInfos => Set<FileInfoEntity>();
    public DbSet<VersionEntity> Versions => Set<VersionEntity>();
    public DbSet<VersionFileMapEntity> VersionFileMaps => Set<VersionFileMapEntity>();
    public DbSet<AttachmentMapEntity> AttachmentMaps => Set<AttachmentMapEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ItemConfiguration());
        modelBuilder.ApplyConfiguration(new BomConfiguration());
        modelBuilder.ApplyConfiguration(new RefDesigConfiguration());
        modelBuilder.ApplyConfiguration(new RevConfiguration());
        modelBuilder.ApplyConfiguration(new ChangeConfiguration());
        modelBuilder.ApplyConfiguration(new ListEntryConfiguration());
        modelBuilder.ApplyConfiguration(new FilesConfiguration());
        modelBuilder.ApplyConfiguration(new FileInfoConfiguration());
        modelBuilder.ApplyConfiguration(new VersionConfiguration());
        modelBuilder.ApplyConfiguration(new VersionFileMapConfiguration());
        modelBuilder.ApplyConfiguration(new AttachmentMapConfiguration());
    }
}
