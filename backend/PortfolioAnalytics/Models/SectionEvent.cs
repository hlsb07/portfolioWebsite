namespace PortfolioAnalytics.Models;

/// <summary>
/// Tracks time spent viewing specific sections of the portfolio
/// </summary>
public class SectionEvent
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    /// <summary>
    /// Name of the section (e.g., "hero", "about", "projects", "resume", "contact")
    /// </summary>
    public string SectionName { get; set; } = null!;

    /// <summary>
    /// How long the visitor viewed this section in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// When the visitor entered this section
    /// </summary>
    public DateTime Timestamp { get; set; }
}
