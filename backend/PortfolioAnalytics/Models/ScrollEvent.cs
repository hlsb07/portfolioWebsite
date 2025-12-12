namespace PortfolioAnalytics.Models;

/// <summary>
/// Tracks how far down the page a visitor scrolled
/// </summary>
public class ScrollEvent
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    /// <summary>
    /// Percentage of page scrolled (0-100)
    /// </summary>
    public int ScrollDepthPercent { get; set; }

    /// <summary>
    /// When this scroll depth was reached
    /// </summary>
    public DateTime Timestamp { get; set; }
}
