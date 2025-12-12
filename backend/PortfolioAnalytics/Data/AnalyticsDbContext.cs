using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Data;

/// <summary>
/// Database context for portfolio analytics
/// Handles all database operations for tracking visitor behavior in a GDPR-compliant manner
/// </summary>
public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<ScrollEvent> ScrollEvents => Set<ScrollEvent>();
    public DbSet<SectionEvent> SectionEvents => Set<SectionEvent>();
    public DbSet<DeviceInfo> DeviceInfos => Set<DeviceInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Visitor configuration
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AnonymousIdHash).IsUnique();
            entity.Property(e => e.AnonymousIdHash).HasMaxLength(64); // SHA-256 produces 64 hex characters
            entity.Property(e => e.FirstSeen).HasDefaultValueSql("NOW()");
            entity.Property(e => e.LastSeen).HasDefaultValueSql("NOW()");
        });

        // Visit configuration
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VisitorId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Page).HasMaxLength(500);
            entity.Property(e => e.Referrer).HasMaxLength(1000);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Visitor)
                  .WithMany(v => v.Visits)
                  .HasForeignKey(e => e.VisitorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ScrollEvent configuration
        modelBuilder.Entity<ScrollEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VisitId);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Visit)
                  .WithMany(v => v.ScrollEvents)
                  .HasForeignKey(e => e.VisitId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SectionEvent configuration
        modelBuilder.Entity<SectionEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VisitId);
            entity.HasIndex(e => e.SectionName);
            entity.Property(e => e.SectionName).HasMaxLength(100);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Visit)
                  .WithMany(v => v.SectionEvents)
                  .HasForeignKey(e => e.VisitId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DeviceInfo configuration
        modelBuilder.Entity<DeviceInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VisitorId);
            entity.Property(e => e.BrowserFamily).HasMaxLength(100);
            entity.Property(e => e.BrowserVersion).HasMaxLength(50);
            entity.Property(e => e.OSFamily).HasMaxLength(100);
            entity.Property(e => e.OSVersion).HasMaxLength(50);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.FirstSeen).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Visitor)
                  .WithMany(v => v.DeviceInfos)
                  .HasForeignKey(e => e.VisitorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
