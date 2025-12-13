namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Data transfer object for tracking section viewing
/// </summary>
public class SectionDto
{
    /// <summary>
    /// Session ID (UUID) from cookie
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// Section name (e.g., "hero", "about", "projects")
    /// </summary>
    public string SectionName { get; set; } = null!;

    /// <summary>
    /// Duration spent in this section in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}
