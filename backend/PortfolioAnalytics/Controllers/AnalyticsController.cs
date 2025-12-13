using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortfolioAnalytics.Data;
using PortfolioAnalytics.DTOs;
using PortfolioAnalytics.Models;
using PortfolioAnalytics.Services;

namespace PortfolioAnalytics.Controllers;

/// <summary>
/// Controller for handling GDPR-compliant, session-based analytics tracking
/// </summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsDbContext _db;
    private readonly SessionService _sessionService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        AnalyticsDbContext db,
        SessionService sessionService,
        ILogger<AnalyticsController> logger)
    {
        _db = db;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Cookieless, aggregated page view tracking for essential-only mode.
    /// Stores only counts per date/path/device with no identifiers.
    /// </summary>
    [HttpPost("basic")]
    public async Task<IActionResult> TrackBasicPageView([FromBody] BasicPageViewDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Path))
        {
            return BadRequest("Path is required");
        }

        var normalizedPath = dto.Path.Trim();
        if (normalizedPath.Length > 500)
        {
            normalizedPath = normalizedPath[..500];
        }
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        var deviceCategory = NormalizeDeviceCategory(dto.Device);
        var today = DateTime.UtcNow.Date;

        try
        {
            var aggregate = await _db.BasicPageViewAggregates
                .FirstOrDefaultAsync(a =>
                    a.Date == today &&
                    a.Path == normalizedPath &&
                    a.DeviceCategory == deviceCategory);

            if (aggregate == null)
            {
                aggregate = new BasicPageViewAggregate
                {
                    Date = today,
                    Path = normalizedPath,
                    DeviceCategory = deviceCategory,
                    Count = 1,
                    LastSeenAt = DateTime.UtcNow
                };

                _db.BasicPageViewAggregates.Add(aggregate);
            }
            else
            {
                aggregate.Count += 1;
                aggregate.LastSeenAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // No content returned to avoid echoing data back to the client
            return NoContent();
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Handle race conditions on the unique index gracefully
            _logger.LogWarning(dbEx, "Duplicate basic page view aggregate detected for {Path} ({Device})", normalizedPath, deviceCategory);

            var aggregate = await _db.BasicPageViewAggregates
                .FirstOrDefaultAsync(a =>
                    a.Date == today &&
                    a.Path == normalizedPath &&
                    a.DeviceCategory == deviceCategory);

            if (aggregate != null)
            {
                aggregate.Count += 1;
                aggregate.LastSeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            _logger.LogWarning(pgEx,
                "Basic page view table missing. Run database migrations to enable essential-only tracking.");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking basic page view for {Path}", normalizedPath);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Tracks a new page visit
    /// </summary>
    [HttpPost("visit")]
    public async Task<IActionResult> TrackVisit([FromBody] VisitDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.Page))
            {
                return BadRequest("SessionId and Page are required");
            }

            // Validate/create session
            var session = await _sessionService.GetOrCreateSessionAsync(dto.SessionId, dto.UserAgent);
            if (session == null)
            {
                return BadRequest("Session expired or invalid");
            }

            var visit = new Visit
            {
                SessionId = session.Id,
                Page = dto.Page,
                Referrer = dto.Referrer,
                Timestamp = DateTime.UtcNow,
                DurationMs = 0 // Will be updated when visit ends
            };

            _db.Visits.Add(visit);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Tracked visit for session: {SessionId}, page: {Page}", dto.SessionId, dto.Page);

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
            if (string.IsNullOrWhiteSpace(dto.SessionId))
            {
                return BadRequest("SessionId is required");
            }

            // Validate session
            if (!await _sessionService.ValidateSessionAsync(dto.SessionId))
            {
                return BadRequest("Session expired or invalid");
            }

            var session = await _db.Sessions
                .Include(s => s.Visits)
                .FirstOrDefaultAsync(s => s.SessionId == dto.SessionId);

            if (session == null)
            {
                return NotFound("Session not found");
            }

            // Get the most recent visit
            var visit = session.Visits.OrderByDescending(v => v.Timestamp).FirstOrDefault();
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

            _logger.LogInformation("Tracked scroll: {Depth}% for session {SessionId}", normalizedDepth, dto.SessionId);

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
            if (string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.SectionName))
            {
                return BadRequest("SessionId and SectionName are required");
            }

            // Validate session
            if (!await _sessionService.ValidateSessionAsync(dto.SessionId))
            {
                return BadRequest("Session expired or invalid");
            }

            var session = await _db.Sessions
                .Include(s => s.Visits)
                .FirstOrDefaultAsync(s => s.SessionId == dto.SessionId);

            if (session == null)
            {
                return NotFound("Session not found");
            }

            // Get the most recent visit
            var visit = session.Visits.OrderByDescending(v => v.Timestamp).FirstOrDefault();
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

            _logger.LogInformation("Tracked section: {Section} ({Duration}ms) for session {SessionId}",
                dto.SectionName, dto.DurationMs, dto.SessionId);

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
            if (string.IsNullOrWhiteSpace(dto.SessionId))
            {
                return BadRequest("SessionId is required");
            }

            var session = await _db.Sessions
                .Include(s => s.Visits)
                .FirstOrDefaultAsync(s => s.SessionId == dto.SessionId);

            if (session == null)
            {
                return NotFound("Session not found");
            }

            // Get the most recent visit
            var visit = session.Visits.OrderByDescending(v => v.Timestamp).FirstOrDefault();
            if (visit == null)
            {
                return NotFound("No active visit found");
            }

            visit.DurationMs = dto.DurationMs;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Ended visit with duration: {Duration}ms for session {SessionId}",
                dto.DurationMs, dto.SessionId);

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
    /// NOW WITH TIME-BASED FILTERING
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] string? password,
        [FromQuery] string? period = "week", // all, today, week, month
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
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

            // Determine date range
            var now = DateTime.UtcNow;
            DateTime filterStart = period switch
            {
                "today" => now.Date,
                "week" => now.AddDays(-7),
                "month" => now.AddDays(-30),
                _ => DateTime.MinValue // "all"
            };

            if (startDate.HasValue) filterStart = startDate.Value;
            var filterEnd = endDate ?? now;

            // Query sessions and visits in date range
            var sessions = await _db.Sessions
                .Where(s => s.CreatedAt >= filterStart && s.CreatedAt <= filterEnd)
                .Include(s => s.Visits)
                .ToListAsync();

            var visits = sessions.SelectMany(s => s.Visits).ToList();
            var visitIds = visits.Select(v => v.Id).ToList();

            // Calculate metrics
            var totalSessions = sessions.Count;
            var totalVisits = visits.Count;

            var avgScrollDepth = await _db.ScrollEvents
                .Where(e => e.Timestamp >= filterStart && e.Timestamp <= filterEnd)
                .AnyAsync()
                    ? (int)await _db.ScrollEvents
                        .Where(e => e.Timestamp >= filterStart && e.Timestamp <= filterEnd)
                        .AverageAsync(e => e.ScrollDepthPercent)
                    : 0;

            var avgTime = visits.Where(v => v.DurationMs > 0).Any()
                ? (int)(visits.Where(v => v.DurationMs > 0).Average(v => v.DurationMs) / 1000)
                : 0;

            // Bounce rate: sessions with < 10 seconds total visit duration
            var bounceCount = visits.Count(v => v.DurationMs < 10000);
            var bounceRate = totalSessions > 0
                ? (int)(bounceCount / (double)totalSessions * 100)
                : 0;

            // Completion rate: sessions with >=100% scroll OR >60s duration
            var completedVisits = visits.Where(v =>
                v.DurationMs > 60000 ||
                _db.ScrollEvents.Any(e => e.VisitId == v.Id && e.ScrollDepthPercent >= 100)
            ).Count();

            var completionRate = totalSessions > 0
                ? (int)(completedVisits / (double)totalSessions * 100)
                : 0;

            // Device breakdown
            var deviceStats = sessions
                .GroupBy(s => s.DeviceCategory ?? "Unknown")
                .Select(g => new { DeviceType = g.Key, Count = g.Count() })
                .ToList();

            // Browser breakdown
            var browserStats = sessions
                .GroupBy(s => s.BrowserFamily ?? "Unknown")
                .Select(g => new { Browser = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            // Section metrics
            var sectionMetrics = await _db.SectionEvents
                .Where(e => e.Timestamp >= filterStart && e.Timestamp <= filterEnd)
                .GroupBy(e => e.SectionName)
                .Select(g => new
                {
                    Section = g.Key,
                    Views = g.Count(),
                    AvgDurationSeconds = (int)(g.Average(e => e.DurationMs) / 1000)
                })
                .OrderByDescending(x => x.AvgDurationSeconds)
                .ToListAsync();

            var topSection = sectionMetrics.FirstOrDefault()?.Section ?? "N/A";

            // Recent visits (last 10)
            var recentVisits = visits
                .OrderByDescending(v => v.Timestamp)
                .Take(10)
                .Select(v => new
                {
                    v.Page,
                    v.Timestamp,
                    DurationSeconds = v.DurationMs / 1000
                })
                .ToList();

            // Daily trend data (for chart)
            var dailyTrend = sessions
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Sessions = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Essential-only (cookieless) traffic overview
            var essentialStats = await GetEssentialOnlyStatsAsync(filterStart.Date, filterEnd.Date);

            return Ok(new
            {
                period,
                startDate = filterStart,
                endDate = filterEnd,
                sessions = totalSessions,
                visits = totalVisits,
                avgScroll = avgScrollDepth,
                avgTime,
                bounceRate,
                completionRate,
                topSection,
                deviceStats,
                browserStats,
                sectionMetrics,
                recentVisits,
                dailyTrend,
                essentialPageViews = essentialStats.PageViews,
                essentialDeviceStats = essentialStats.DeviceStats,
                essentialPathStats = essentialStats.PathStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return StatusCode(500, "Internal server error");
        }
    }

    private static string NormalizeDeviceCategory(string? device)
    {
        return device?.Trim().ToLowerInvariant() switch
        {
            "mobile" => "mobile",
            "desktop" => "desktop",
            "tablet" => "tablet",
            _ => "unknown"
        };
    }

    private async Task<(int PageViews, List<object> DeviceStats, List<object> PathStats)> GetEssentialOnlyStatsAsync(
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var essentialQuery = _db.BasicPageViewAggregates
                .Where(v => v.Date >= startDate && v.Date <= endDate);

            var pageViews = await essentialQuery.SumAsync(v => (int?)v.Count) ?? 0;

            var deviceStats = await essentialQuery
                .GroupBy(v => v.DeviceCategory)
                .Select(g => new
                {
                    DeviceType = g.Key,
                    Count = g.Sum(x => x.Count)
                })
                .OrderByDescending(x => x.Count)
                .Cast<object>()
                .ToListAsync();

            var pathStats = await essentialQuery
                .GroupBy(v => v.Path)
                .Select(g => new
                {
                    Path = g.Key,
                    Count = g.Sum(x => x.Count)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .Cast<object>()
                .ToListAsync();

            return (pageViews, deviceStats, pathStats);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            _logger.LogWarning(ex,
                "BasicPageViewAggregates table missing while loading essential-only stats. Apply latest migrations when convenient.");
            return (0, new List<object>(), new List<object>());
        }
    }
}
