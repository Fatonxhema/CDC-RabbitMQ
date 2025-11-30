using CDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CDC.Infrastructure.Persistence;

public class CdcDbContext : DbContext
{
    public CdcDbContext(DbContextOptions<CdcDbContext> options) : base(options) { }

    public DbSet<CdcEvent> CdcEvents { get; set; }
    public DbSet<RoutingConfiguration> RoutingConfigurations { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CdcEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.PartitionKey, e.SequenceNumber });
            entity.Property(e => e.Payload).HasColumnType("jsonb");
        });

        modelBuilder.Entity<RoutingConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TableName).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });
    }
}
