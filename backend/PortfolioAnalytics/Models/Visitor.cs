namespace PortfolioAnalytics.Models;

/// <summary>
/// Represents an anonymous visitor identified by a cryptographic hash.
/// This model ensures GDPR compliance by not storing any personally identifiable information.
/// </summary>
public class Visitor
{
    public int Id { get; set; }

    /// <summary>
    /// SHA-256 hash of (UserAgent + ServerSecret) - ensures anonymity while allowing visitor recognition
    /// </summary>
    public string AnonymousIdHash { get; set; } = null!;

    /// <summary>
    /// Timestamp of first visit
    /// </summary>
    public DateTime FirstSeen { get; set; }

    /// <summary>
    /// Timestamp of most recent visit
    /// </summary>
    public DateTime LastSeen { get; set; }

    // Navigation properties
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
    public ICollection<DeviceInfo> DeviceInfos { get; set; } = new List<DeviceInfo>();
}
