namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Data transfer object for tracking visit end
/// </summary>
public class EndDto
{
    /// <summary>
    /// Anonymous visitor ID hash
    /// </summary>
    public string AnonymousIdHash { get; set; } = null!;

    /// <summary>
    /// Total duration of the visit in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}
