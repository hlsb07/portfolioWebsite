namespace PortfolioAnalytics.Models;

/// <summary>
/// Aggregated, cookieless page view counters for essential-only mode.
/// Stores only coarse counts per date/path/device with no identifiers.
/// </summary>
public class BasicPageViewAggregate
{
    public int Id { get; set; }

    /// <summary>
    /// UTC date bucket for the page view (00:00 UTC)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Path visited (trimmed to max length in DB config)
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// Coarse device category supplied by client (mobile/desktop/tablet/unknown)
    /// </summary>
    public string DeviceCategory { get; set; } = "unknown";

    /// <summary>
    /// Aggregated count of visits for this date/path/device
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Last time this aggregate was incremented
    /// </summary>
    public DateTime LastSeenAt { get; set; }
}
