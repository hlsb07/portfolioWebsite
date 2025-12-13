using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Data;

namespace PortfolioAnalytics.Services;

/// <summary>
/// Background service that runs daily to enforce data retention policies
/// Aggregates data before deletion and cleans up expired sessions
/// </summary>
public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataRetentionBackgroundService> _logger;

    // Retention policies
    private const int RAW_DATA_RETENTION_DAYS = 14;
    private const int AGGREGATE_RETENTION_DAYS = 30;
    private const int BASIC_PAGE_VIEW_RETENTION_DAYS = 365;

    // Run daily at 2 AM UTC
    private readonly TimeSpan _scheduleTime = new TimeSpan(2, 0, 0);

    public DataRetentionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DataRetentionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Retention Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = now.Date.Add(_scheduleTime);

                if (now > nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;

                _logger.LogInformation("Next cleanup scheduled for: {NextRun} UTC", nextRun);
                await Task.Delay(delay, stoppingToken);

                // Execute cleanup and aggregation
                await RunCleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data retention background service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour
            }
        }
    }

    private async Task RunCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var aggregationService = scope.ServiceProvider.GetRequiredService<AggregationService>();

        _logger.LogInformation("Starting data retention cleanup");

        // Step 1: Aggregate yesterday's data (if not already done)
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        try
        {
            await aggregationService.AggregateForDateAsync(yesterday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating yesterday's data");
        }

        // Step 2: Delete raw data older than retention period
        var rawDataCutoff = DateTime.UtcNow.AddDays(-RAW_DATA_RETENTION_DAYS);

        try
        {
            // Delete old sessions (cascade will delete visits, scroll events, section events)
            var oldSessions = await db.Sessions
                .Where(s => s.CreatedAt < rawDataCutoff)
                .ToListAsync();

            if (oldSessions.Any())
            {
                db.Sessions.RemoveRange(oldSessions);
                await db.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} old sessions (older than {Days} days)",
                    oldSessions.Count, RAW_DATA_RETENTION_DAYS);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting raw data");
        }

        // Step 3: Delete aggregates older than retention period
        var aggregateCutoff = DateTime.UtcNow.AddDays(-AGGREGATE_RETENTION_DAYS).Date;

        try
        {
            var oldDailyAggregates = await db.DailyAggregates
                .Where(a => a.Date < aggregateCutoff)
                .ToListAsync();

            if (oldDailyAggregates.Any())
            {
                db.DailyAggregates.RemoveRange(oldDailyAggregates);
                await db.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} old daily aggregates (older than {Days} days)",
                    oldDailyAggregates.Count, AGGREGATE_RETENTION_DAYS);
            }

            var oldWeeklyAggregates = await db.WeeklyAggregates
                .Where(a => a.WeekStartDate < aggregateCutoff)
                .ToListAsync();

            if (oldWeeklyAggregates.Any())
            {
                db.WeeklyAggregates.RemoveRange(oldWeeklyAggregates);
                await db.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} old weekly aggregates (older than {Days} days)",
                    oldWeeklyAggregates.Count, AGGREGATE_RETENTION_DAYS);
            }
        }
        catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting aggregates");
            }

            // Step 3b: Delete basic page view aggregates older than retention period (cookieless counts)
            var basicPageViewCutoff = DateTime.UtcNow.AddDays(-BASIC_PAGE_VIEW_RETENTION_DAYS).Date;
            try
            {
                var oldBasicPageViews = await db.BasicPageViewAggregates
                    .Where(v => v.Date < basicPageViewCutoff)
                    .ToListAsync();

                if (oldBasicPageViews.Any())
                {
                    db.BasicPageViewAggregates.RemoveRange(oldBasicPageViews);
                    await db.SaveChangesAsync();
                    _logger.LogInformation(
                        "Deleted {Count} basic page view aggregates (older than {Days} days)",
                        oldBasicPageViews.Count,
                        BASIC_PAGE_VIEW_RETENTION_DAYS);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old basic page view aggregates");
            }

            // Step 4: Delete expired sessions (housekeeping)
            try
            {
                var expiredSessions = await db.Sessions
                    .Where(s => s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredSessions.Any())
            {
                db.Sessions.RemoveRange(expiredSessions);
                await db.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} expired sessions", expiredSessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired sessions");
        }

        _logger.LogInformation("Data retention cleanup completed");
    }
}
