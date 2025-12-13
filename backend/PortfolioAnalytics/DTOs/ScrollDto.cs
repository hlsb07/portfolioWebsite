namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Data transfer object for tracking scroll depth
/// </summary>
public class ScrollDto
{
    /// <summary>
    /// Session ID (UUID) from cookie
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// Scroll depth as a percentage (0-100)
    /// </summary>
    public int ScrollDepthPercent { get; set; }
}
