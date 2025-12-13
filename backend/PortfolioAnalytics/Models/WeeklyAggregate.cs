namespace PortfolioAnalytics.Models;

/// <summary>
/// Aggregated analytics data per week (ISO week)
/// Provides weekly trend analysis while maintaining data retention policy
/// </summary>
public class WeeklyAggregate
{
    public int Id { get; set; }

    /// <summary>
    /// Year (e.g., 2025)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// ISO week number (1-53)
    /// </summary>
    public int WeekNumber { get; set; }

    /// <summary>
    /// First day of the week (Monday)
    /// </summary>
    public DateTime WeekStartDate { get; set; }

    // Same metrics as DailyAggregate
    public int TotalSessions { get; set; }
    public int TotalVisits { get; set; }
    public double AvgScrollDepthPercent { get; set; }
    public double AvgSessionDurationMs { get; set; }
    public double AvgTimeOnPageMs { get; set; }
    public int DesktopSessions { get; set; }
    public int MobileSessions { get; set; }
    public int TabletSessions { get; set; }

    /// <summary>
    /// Browser distribution as JSON
    /// Example: {"Chrome": 150, "Firefox": 80, "Safari": 45}
    /// </summary>
    public string? BrowserBreakdownJson { get; set; }

    public int BounceCount { get; set; }
    public int CompletedCount { get; set; }

    /// <summary>
    /// Section viewing metrics as JSON
    /// </summary>
    public string? SectionMetricsJson { get; set; }

    /// <summary>
    /// When this aggregate was calculated
    /// </summary>
    public DateTime AggregatedAt { get; set; }
}
