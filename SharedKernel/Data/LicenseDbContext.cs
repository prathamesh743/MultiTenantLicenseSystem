using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace SharedKernel.Data;

public class LicenseDbContext : DbContext
{
    public DbSet<License> Licenses { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<PaymentRecord> Payments { get; set; } = null!;

    public string TenantId { get; set; } = string.Empty;

    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Multi-tenancy global query filter (this is what enforces isolation per tenant)
        modelBuilder.Entity<License>()
            .HasQueryFilter(l => l.TenantId == TenantId);

        modelBuilder.Entity<Document>()
            .HasQueryFilter(d => d.TenantId == TenantId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == TenantId);

        modelBuilder.Entity<Notification>()
            .HasQueryFilter(n => n.TenantId == TenantId);

        modelBuilder.Entity<PaymentRecord>()
            .HasQueryFilter(p => p.TenantId == TenantId);

        // Explicitly map TenantId as a required column
        modelBuilder.Entity<License>()
            .Property(l => l.TenantId)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<Document>()
            .Property(d => d.TenantId)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<User>()
            .Property(u => u.TenantId)
            .IsRequired()
            .HasMaxLength(100);

        // Optional: nice table names
        modelBuilder.Entity<License>().ToTable("Licenses");
        modelBuilder.Entity<Document>().ToTable("Documents");
        modelBuilder.Entity<User>().ToTable("Users");

        base.OnModelCreating(modelBuilder);
    }
}