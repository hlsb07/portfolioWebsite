namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Data transfer object for tracking a page visit
/// </summary>
public class VisitDto
{
    /// <summary>
    /// Anonymous visitor ID hash generated on the client
    /// </summary>
    public string AnonymousIdHash { get; set; } = null!;

    /// <summary>
    /// Page path being visited
    /// </summary>
    public string Page { get; set; } = null!;

    /// <summary>
    /// Referrer URL (where the visitor came from)
    /// </summary>
    public string? Referrer { get; set; }

    /// <summary>
    /// User-Agent string for device detection
    /// </summary>
    public string? UserAgent { get; set; }
}
