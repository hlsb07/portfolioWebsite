using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Data;

/// <summary>
/// Database context for portfolio analytics
/// Handles session-based, GDPR-compliant tracking with data retention policies
/// </summary>
public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<ScrollEvent> ScrollEvents => Set<ScrollEvent>();
    public DbSet<SectionEvent> SectionEvents => Set<SectionEvent>();
    public DbSet<DailyAggregate> DailyAggregates => Set<DailyAggregate>();
    public DbSet<WeeklyAggregate> WeeklyAggregates => Set<WeeklyAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.SessionId).HasMaxLength(36); // UUID length
            entity.Property(e => e.DeviceCategory).HasMaxLength(50);
            entity.Property(e => e.BrowserFamily).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.LastActivity).HasDefaultValueSql("NOW()");
        });

        // Visit configuration
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Page).HasMaxLength(500);
            entity.Property(e => e.Referrer).HasMaxLength(1000);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Visits)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ScrollEvent configuration
        modelBuilder.Entity<ScrollEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VisitId);
            entity.HasIndex(e => e.Timestamp);
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
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.SectionName).HasMaxLength(100);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Visit)
                  .WithMany(v => v.SectionEvents)
                  .HasForeignKey(e => e.VisitId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DailyAggregate configuration
        modelBuilder.Entity<DailyAggregate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Date).IsUnique();
            entity.Property(e => e.BrowserBreakdownJson).HasColumnType("jsonb");
            entity.Property(e => e.SectionMetricsJson).HasColumnType("jsonb");
            entity.Property(e => e.AggregatedAt).HasDefaultValueSql("NOW()");
        });

        // WeeklyAggregate configuration
        modelBuilder.Entity<WeeklyAggregate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Year, e.WeekNumber }).IsUnique();
            entity.Property(e => e.BrowserBreakdownJson).HasColumnType("jsonb");
            entity.Property(e => e.SectionMetricsJson).HasColumnType("jsonb");
            entity.Property(e => e.AggregatedAt).HasDefaultValueSql("NOW()");
        });
    }
}
