namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Minimal, cookieless page view payload for essential-only mode
/// Contains no identifiers and is aggregated server-side.
/// </summary>
public class BasicPageViewDto
{
    /// <summary>
    /// Page path being viewed (e.g., /portfolio)
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// Coarse device category derived client-side from viewport (mobile/desktop/tablet)
    /// </summary>
    public string? Device { get; set; }
}
