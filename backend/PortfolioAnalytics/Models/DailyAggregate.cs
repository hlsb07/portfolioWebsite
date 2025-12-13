namespace PortfolioAnalytics.Models;

/// <summary>
/// Aggregated analytics data per day
/// Used after raw data is deleted (retention policy)
/// Allows analytics reporting while respecting data minimization
/// </summary>
public class DailyAggregate
{
    public int Id { get; set; }

    /// <summary>
    /// Date of aggregation (date only, time = 00:00:00)
    /// Example: 2025-12-12 00:00:00
    /// </summary>
    public DateTime Date { get; set; }

    // Session metrics
    public int TotalSessions { get; set; }
    public int TotalVisits { get; set; }

    // Engagement metrics
    public double AvgScrollDepthPercent { get; set; }
    public double AvgSessionDurationMs { get; set; }
    public double AvgTimeOnPageMs { get; set; }

    // Device breakdown
    public int DesktopSessions { get; set; }
    public int MobileSessions { get; set; }
    public int TabletSessions { get; set; }

    // Browser breakdown (stored as JSON)
    /// <summary>
    /// Browser distribution as JSON
    /// Example: {"Chrome": 45, "Firefox": 23, "Safari": 15}
    /// </summary>
    public string? BrowserBreakdownJson { get; set; }

    // Behavior metrics
    /// <summary>
    /// Number of sessions with less than 10 seconds duration (bounce)
    /// </summary>
    public int BounceCount { get; set; }

    /// <summary>
    /// Number of sessions with >60s duration OR 100% scroll depth (completed)
    /// </summary>
    public int CompletedCount { get; set; }

    // Section metrics (stored as JSON)
    /// <summary>
    /// Section viewing metrics as JSON
    /// Example: {"hero": {"views": 100, "avgDuration": 5000}, "projekte": {"views": 80, "avgDuration": 12000}}
    /// </summary>
    public string? SectionMetricsJson { get; set; }

    /// <summary>
    /// When this aggregate was calculated
    /// </summary>
    public DateTime AggregatedAt { get; set; }
}
