namespace PortfolioAnalytics.Models;

/// <summary>
/// Stores anonymized browser and device information for analytics
/// This is GDPR-compliant as it only stores general device categories, not unique identifiers
/// </summary>
public class DeviceInfo
{
    public int Id { get; set; }

    public int VisitorId { get; set; }
    public Visitor Visitor { get; set; } = null!;

    /// <summary>
    /// Browser family (e.g., "Chrome", "Firefox", "Safari")
    /// </summary>
    public string BrowserFamily { get; set; } = null!;

    /// <summary>
    /// Browser version (e.g., "120.0")
    /// </summary>
    public string? BrowserVersion { get; set; }

    /// <summary>
    /// Operating system family (e.g., "Windows", "macOS", "Linux", "iOS", "Android")
    /// </summary>
    public string OSFamily { get; set; } = null!;

    /// <summary>
    /// OS version (e.g., "10", "14.2")
    /// </summary>
    public string? OSVersion { get; set; }

    /// <summary>
    /// Device type (e.g., "Desktop", "Mobile", "Tablet")
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// When this device info was first recorded
    /// </summary>
    public DateTime FirstSeen { get; set; }
}
