namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Data transfer object for tracking scroll depth
/// </summary>
public class ScrollDto
{
    /// <summary>
    /// Anonymous visitor ID hash
    /// </summary>
    public string AnonymousIdHash { get; set; } = null!;

    /// <summary>
    /// Scroll depth as a percentage (0-100)
    /// </summary>
    public int ScrollDepthPercent { get; set; }
}
