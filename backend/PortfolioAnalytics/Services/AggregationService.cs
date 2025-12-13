using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Data;
using PortfolioAnalytics.Models;
using System.Text.Json;

namespace PortfolioAnalytics.Services;

/// <summary>
/// Service for aggregating raw analytics data before deletion
/// Supports data retention policy while maintaining useful statistics
/// </summary>
public class AggregationService
{
    private readonly AnalyticsDbContext _db;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(AnalyticsDbContext db, ILogger<AggregationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates data for a specific date
    /// </summary>
    public async Task AggregateForDateAsync(DateTime date)
    {
        var dateOnly = date.Date;
        var nextDay = dateOnly.AddDays(1);

        // Check if already aggregated
        var existing = await _db.DailyAggregates
            .FirstOrDefaultAsync(a => a.Date == dateOnly);

        if (existing != null)
        {
            _logger.LogInformation("Data already aggregated for {Date}", dateOnly);
            return;
        }

        // Get sessions for this date
        var sessions = await _db.Sessions
            .Where(s => s.CreatedAt >= dateOnly && s.CreatedAt < nextDay)
            .Include(s => s.Visits)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            _logger.LogInformation("No sessions to aggregate for {Date}", dateOnly);
            return;
        }

        var visits = sessions.SelectMany(s => s.Visits).ToList();
        var visitIds = visits.Select(v => v.Id).ToList();

        // Get scroll events
        var scrollEvents = await _db.ScrollEvents
            .Where(e => visitIds.Contains(e.VisitId))
            .ToListAsync();

        // Get section events
        var sectionEvents = await _db.SectionEvents
            .Where(e => visitIds.Contains(e.VisitId))
            .ToListAsync();

        // Calculate metrics
        var aggregate = new DailyAggregate
        {
            Date = dateOnly,
            TotalSessions = sessions.Count,
            TotalVisits = visits.Count,

            AvgScrollDepthPercent = scrollEvents.Any()
                ? scrollEvents.Average(e => e.ScrollDepthPercent)
                : 0,

            AvgSessionDurationMs = visits.Where(v => v.DurationMs > 0).Any()
                ? visits.Where(v => v.DurationMs > 0).Average(v => v.DurationMs)
                : 0,

            AvgTimeOnPageMs = visits.Where(v => v.DurationMs > 0).Any()
                ? visits.Where(v => v.DurationMs > 0).Average(v => v.DurationMs)
                : 0,

            DesktopSessions = sessions.Count(s => s.DeviceCategory == "Desktop"),
            MobileSessions = sessions.Count(s => s.DeviceCategory == "Mobile"),
            TabletSessions = sessions.Count(s => s.DeviceCategory == "Tablet"),

            BounceCount = visits.Count(v => v.DurationMs < 10000),
            CompletedCount = visits.Count(v => v.DurationMs > 60000 ||
                scrollEvents.Any(e => e.VisitId == v.Id && e.ScrollDepthPercent >= 100)),

            AggregatedAt = DateTime.UtcNow
        };

        // Browser breakdown JSON
        var browserBreakdown = sessions
            .GroupBy(s => s.BrowserFamily ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
        aggregate.BrowserBreakdownJson = JsonSerializer.Serialize(browserBreakdown);

        // Section metrics JSON
        var sectionMetrics = sectionEvents
            .GroupBy(e => e.SectionName)
            .Select(g => new
            {
                Section = g.Key,
                Views = g.Count(),
                AvgDuration = g.Average(e => e.DurationMs)
            })
            .ToDictionary(x => x.Section, x => new { x.Views, x.AvgDuration });
        aggregate.SectionMetricsJson = JsonSerializer.Serialize(sectionMetrics);

        _db.DailyAggregates.Add(aggregate);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Aggregated data for {Date}: {Sessions} sessions, {Visits} visits",
            dateOnly, sessions.Count, visits.Count);
    }
}
