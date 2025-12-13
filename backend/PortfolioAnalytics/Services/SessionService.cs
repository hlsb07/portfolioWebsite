using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Data;
using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Services;

/// <summary>
/// Service for managing anonymous browsing sessions
/// Implements 30-minute sliding window timeout
/// </summary>
public class SessionService
{
    private readonly AnalyticsDbContext _db;
    private readonly ILogger<SessionService> _logger;

    // Session timeout configuration
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _maxSessionDuration = TimeSpan.FromHours(4);

    public SessionService(AnalyticsDbContext db, ILogger<SessionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a session, validates timeout
    /// </summary>
    public async Task<Session?> GetOrCreateSessionAsync(
        string sessionId,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("Session ID is null or empty");
            return null;
        }

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session != null)
        {
            // Check if session expired
            if (DateTime.UtcNow > session.ExpiresAt)
            {
                _logger.LogInformation("Session expired: {SessionId}", sessionId);
                return null; // Session expired, client needs new session
            }

            // Extend session timeout (sliding window)
            session.LastActivity = DateTime.UtcNow;
            session.ExpiresAt = DateTime.UtcNow.Add(_sessionTimeout);

            await _db.SaveChangesAsync();
            return session;
        }

        // Create new session
        var deviceInfo = ParseUserAgentCoarse(userAgent);

        session = new Session
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_sessionTimeout),
            DeviceCategory = deviceInfo.DeviceCategory,
            BrowserFamily = deviceInfo.BrowserFamily
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created new session: {SessionId}", sessionId);
        return session;
    }

    /// <summary>
    /// Validates session exists and not expired
    /// </summary>
    public async Task<bool> ValidateSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null) return false;

        return DateTime.UtcNow <= session.ExpiresAt;
    }

    /// <summary>
    /// Parses User-Agent into COARSE categories (privacy-preserving)
    /// No version numbers, no detailed fingerprinting
    /// </summary>
    private (string DeviceCategory, string BrowserFamily) ParseUserAgentCoarse(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return ("Unknown", "Unknown");

        // Device category (coarse)
        var deviceCategory = "Desktop";
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            deviceCategory = "Mobile";
        else if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
                 userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            deviceCategory = "Tablet";

        // Browser family (coarse - major browsers only, no versions)
        var browserFamily = "Other";
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            browserFamily = "Edge";
        else if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
            browserFamily = "Chrome";
        else if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            browserFamily = "Firefox";
        else if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) &&
                 !userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            browserFamily = "Safari";

        return (deviceCategory, browserFamily);
    }
}
