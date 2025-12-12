namespace PortfolioAnalytics.Models;

/// <summary>
/// Represents a single page visit/session
/// </summary>
public class Visit
{
    public int Id { get; set; }

    public int VisitorId { get; set; }
    public Visitor Visitor { get; set; } = null!;

    /// <summary>
    /// The page path that was visited (e.g., "/", "/projects")
    /// </summary>
    public string Page { get; set; } = null!;

    /// <summary>
    /// When the visit started
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Total duration of the visit in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// URL referrer (where the visitor came from) - optional
    /// </summary>
    public string? Referrer { get; set; }

    // Navigation properties
    public ICollection<ScrollEvent> ScrollEvents { get; set; } = new List<ScrollEvent>();
    public ICollection<SectionEvent> SectionEvents { get; set; } = new List<SectionEvent>();
}
