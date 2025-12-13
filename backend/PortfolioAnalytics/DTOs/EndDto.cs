namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Data transfer object for tracking visit end
/// </summary>
public class EndDto
{
    /// <summary>
    /// Session ID (UUID) from cookie
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// Total duration of the visit in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}
