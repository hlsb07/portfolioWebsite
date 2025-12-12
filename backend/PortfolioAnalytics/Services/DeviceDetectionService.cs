using UAParser;
using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Services;

/// <summary>
/// Service for detecting device, browser, and OS information from User-Agent strings
/// Uses the UAParser library for accurate detection
/// </summary>
public class DeviceDetectionService
{
    private readonly Parser _parser;

    public DeviceDetectionService()
    {
        _parser = Parser.GetDefault();
    }

    /// <summary>
    /// Parses User-Agent string and creates a DeviceInfo object
    /// </summary>
    /// <param name="userAgent">User-Agent string from the browser</param>
    /// <param name="visitorId">ID of the visitor this device belongs to</param>
    /// <returns>DeviceInfo object with parsed information</returns>
    public DeviceInfo ParseUserAgent(string userAgent, int visitorId)
    {
        var clientInfo = _parser.Parse(userAgent);

        return new DeviceInfo
        {
            VisitorId = visitorId,
            BrowserFamily = clientInfo.UA.Family ?? "Unknown",
            BrowserVersion = $"{clientInfo.UA.Major}.{clientInfo.UA.Minor}",
            OSFamily = clientInfo.OS.Family ?? "Unknown",
            OSVersion = $"{clientInfo.OS.Major}.{clientInfo.OS.Minor}",
            DeviceType = DetermineDeviceType(clientInfo),
            FirstSeen = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Determines the device type (Desktop, Mobile, or Tablet) from parsed client info
    /// </summary>
    private string DetermineDeviceType(ClientInfo clientInfo)
    {
        var deviceFamily = clientInfo.Device.Family?.ToLowerInvariant() ?? "";
        var osFamily = clientInfo.OS.Family?.ToLowerInvariant() ?? "";

        // Check if it's a mobile device
        if (deviceFamily.Contains("mobile") ||
            osFamily.Contains("ios") ||
            osFamily.Contains("android"))
        {
            // Distinguish between tablet and phone
            if (deviceFamily.Contains("tablet") ||
                deviceFamily.Contains("ipad") ||
                (osFamily.Contains("android") && !deviceFamily.Contains("mobile")))
            {
                return "Tablet";
            }
            return "Mobile";
        }

        return "Desktop";
    }
}
