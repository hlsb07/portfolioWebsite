namespace PortfolioAnalytics.Models;

/// <summary>
/// Represents an anonymous browsing session (30-minute timeout)
/// No cross-day tracking - each session is independent
/// GDPR-compliant: stores only session UUID and coarse device info
/// </summary>
public class Session
{
    public int Id { get; set; }

    /// <summary>
    /// Anonymous session identifier (UUID from cookie)
    /// Example: "550e8400-e29b-41d4-a716-446655440000"
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last activity timestamp (for timeout calculation)
    /// Updated on every request (sliding window)
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Session expiration timestamp (hard limit)
    /// CreatedAt + 30 minutes (sliding window)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Coarse device category (Desktop, Mobile, Tablet) - privacy-preserving
    /// No specific device models or versions
    /// </summary>
    public string? DeviceCategory { get; set; }

    /// <summary>
    /// Coarse browser family (Chrome, Firefox, Safari, Edge, Other) - privacy-preserving
    /// No version numbers or detailed fingerprinting
    /// </summary>
    public string? BrowserFamily { get; set; }

    // Navigation properties
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
