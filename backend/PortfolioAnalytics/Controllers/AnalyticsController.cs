using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Data;
using PortfolioAnalytics.DTOs;
using PortfolioAnalytics.Models;
using PortfolioAnalytics.Services;

namespace PortfolioAnalytics.Controllers;

/// <summary>
/// Controller for handling GDPR-compliant analytics tracking
/// </summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsDbContext _db;
    private readonly AnonymousIdService _anonymousIdService;
    private readonly DeviceDetectionService _deviceDetectionService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        AnalyticsDbContext db,
        AnonymousIdService anonymousIdService,
        DeviceDetectionService deviceDetectionService,
        ILogger<AnalyticsController> logger)
    {
        _db = db;
        _anonymousIdService = anonymousIdService;
        _deviceDetectionService = deviceDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a visitor based on their anonymous ID hash
    /// Also updates device information if it has changed
    /// </summary>
    private async Task<Visitor?> GetOrCreateVisitorAsync(string anonymousIdHash, string? userAgent)
    {
        var visitor = await _db.Visitors
            .Include(v => v.DeviceInfos)
            .FirstOrDefaultAsync(v => v.AnonymousIdHash == anonymousIdHash);

        if (visitor != null)
        {
            visitor.LastSeen = DateTime.UtcNow;

            // Update device info if User-Agent changed
            if (!string.IsNullOrEmpty(userAgent))
            {
                var hasDevice = visitor.DeviceInfos.Any();
                if (!hasDevice)
                {
                    var deviceInfo = _deviceDetectionService.ParseUserAgent(userAgent, visitor.Id);
                    _db.DeviceInfos.Add(deviceInfo);
                }
            }

            return visitor;
        }

        // Create new visitor
        visitor = new Visitor
        {
            AnonymousIdHash = anonymousIdHash,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        _db.Visitors.Add(visitor);
        await _db.SaveChangesAsync();

        // Add device info for new visitor
        if (!string.IsNullOrEmpty(userAgent))
        {
            var deviceInfo = _deviceDetectionService.ParseUserAgent(userAgent, visitor.Id);
            _db.DeviceInfos.Add(deviceInfo);
            await _db.SaveChangesAsync();
        }

        return visitor;
    }

    /// <summary>
    /// Tracks a new page visit
    /// </summary>
    [HttpPost("visit")]
    public async Task<IActionResult> TrackVisit([FromBody] VisitDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.AnonymousIdHash) || string.IsNullOrWhiteSpace(dto.Page))
            {
                return BadRequest("AnonymousIdHash and Page are required");
            }

            var visitor = await GetOrCreateVisitorAsync(dto.AnonymousIdHash, dto.UserAgent);
            if (visitor == null)
            {
                return BadRequest("Failed to create or retrieve visitor");
            }

            var visit = new Visit
            {
                VisitorId = visitor.Id,
                Page = dto.Page,
                Referrer = dto.Referrer,
                Timestamp = DateTime.UtcNow,
                DurationMs = 0 // Will be updated when visit ends
            };

            _db.Visits.Add(visit);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Tracked visit for page: {Page}", dto.Page);

            return Ok(new { visitId = visit.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking visit");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Tracks scroll depth for the current visit
    /// </summary>
    [HttpPost("scroll")]
    public async Task<IActionResult> TrackScroll([FromBody] ScrollDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.AnonymousIdHash))
            {
                return BadRequest("AnonymousIdHash is required");
            }

            var visitor = await _db.Visitors
                .Include(v => v.Visits)
                .FirstOrDefaultAsync(v => v.AnonymousIdHash == dto.AnonymousIdHash);

            if (visitor == null)
            {
                return NotFound("Visitor not found");
            }

            // Get the most recent visit
            var visit = visitor.Visits.OrderByDescending(v => v.Timestamp).FirstOrDefault();
            if (visit == null)
            {
                return NotFound("No active visit found");
            }

            // Only record scroll events at 25%, 50%, 75%, and 100% milestones
            var milestones = new[] { 25, 50, 75, 100 };
            var normalizedDepth = milestones.OrderBy(m => Math.Abs(m - dto.ScrollDepthPercent)).First();

            // Check if we already recorded this milestone
            var existingEvent = await _db.ScrollEvents
                .FirstOrDefaultAsync(e => e.VisitId == visit.Id && e.ScrollDepthPercent == normalizedDepth);

            if (existingEvent != null)
            {
                return Ok(); // Already recorded this milestone
            }

            var scrollEvent = new ScrollEvent
            {
                VisitId = visit.Id,
                ScrollDepthPercent = normalizedDepth,
                Timestamp = DateTime.UtcNow
            };

            _db.ScrollEvents.Add(scrollEvent);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Tracked scroll: {Depth}%", normalizedDepth);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking scroll");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Tracks time spent viewing a specific section
    /// </summary>
    [HttpPost("section")]
    public async Task<IActionResult> TrackSection([FromBody] SectionDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.AnonymousIdHash) || string.IsNullOrWhiteSpace(dto.SectionName))
            {
                return BadRequest("AnonymousIdHash and SectionName are required");
            }

            var visitor = await _db.Visitors
                .Include(v => v.Visits)
                .FirstOrDefaultAsync(v => v.AnonymousIdHash == dto.AnonymousIdHash);

            if (visitor == null)
            {
                return NotFound("Visitor not found");
            }

            // Get the most recent visit
            var visit = visitor.Visits.OrderByDescending(v => v.Timestamp).FirstOrDefault();
            if (visit == null)
            {
                return NotFound("No active visit found");
            }

            var sectionEvent = new SectionEvent
            {
                VisitId = visit.Id,
                SectionName = dto.SectionName,
                DurationMs = dto.DurationMs,
                Timestamp = DateTime.UtcNow
            };

            _db.SectionEvents.Add(sectionEvent);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Tracked section: {Section} ({Duration}ms)", dto.SectionName, dto.DurationMs);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking section");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates the visit duration when the user leaves the page
    /// </summary>
    [HttpPost("end")]
    public async Task<IActionResult> EndVisit([FromBody] EndDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.AnonymousIdHash))
            {
                return BadRequest("AnonymousIdHash is required");
            }

            var visitor = await _db.Visitors
                .Include(v => v.Visits)
                .FirstOrDefaultAsync(v => v.AnonymousIdHash == dto.AnonymousIdHash);

            if (visitor == null)
            {
                return NotFound("Visitor not found");
            }

            // Get the most recent visit
            var visit = visitor.Visits.OrderByDescending(v => v.Timestamp).FirstOrDefault();
            if (visit == null)
            {
                return NotFound("No active visit found");
            }

            visit.DurationMs = dto.DurationMs;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Ended visit with duration: {Duration}ms", dto.DurationMs);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending visit");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets aggregated analytics statistics for the dashboard
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] string? password)
    {
        try
        {
            // Basic password protection for the dashboard
            var expectedPassword = Environment.GetEnvironmentVariable("ANALYTICS_PASSWORD")
                ?? "change-me-in-production";

            if (password != expectedPassword)
            {
                return Unauthorized("Invalid password");
            }

            var totalVisitors = await _db.Visitors.CountAsync();
            var totalVisits = await _db.Visits.CountAsync();

            var avgScrollDepth = await _db.ScrollEvents.AnyAsync()
                ? (int)await _db.ScrollEvents.AverageAsync(e => e.ScrollDepthPercent)
                : 0;

            var avgTimeSeconds = await _db.Visits.Where(v => v.DurationMs > 0).AnyAsync()
                ? (int)(await _db.Visits.Where(v => v.DurationMs > 0).AverageAsync(v => v.DurationMs) / 1000)
                : 0;

            var topSection = await _db.SectionEvents
                .GroupBy(e => e.SectionName)
                .OrderByDescending(g => g.Sum(e => e.DurationMs))
                .Select(g => g.Key)
                .FirstOrDefaultAsync() ?? "N/A";

            var deviceStats = await _db.DeviceInfos
                .GroupBy(d => d.DeviceType)
                .Select(g => new { DeviceType = g.Key ?? "Unknown", Count = g.Count() })
                .ToListAsync();

            var browserStats = await _db.DeviceInfos
                .GroupBy(d => d.BrowserFamily)
                .Select(g => new { Browser = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var recentVisits = await _db.Visits
                .OrderByDescending(v => v.Timestamp)
                .Take(10)
                .Select(v => new
                {
                    v.Page,
                    v.Timestamp,
                    DurationSeconds = v.DurationMs / 1000
                })
                .ToListAsync();

            return Ok(new
            {
                visitors = totalVisitors,
                visits = totalVisits,
                avgScroll = avgScrollDepth,
                avgTime = avgTimeSeconds,
                topSection,
                deviceStats,
                browserStats,
                recentVisits
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return StatusCode(500, "Internal server error");
        }
    }
}
