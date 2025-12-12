using System.Security.Cryptography;
using System.Text;

namespace PortfolioAnalytics.Services;

/// <summary>
/// Service for handling anonymous visitor identification in a GDPR-compliant way
/// Uses cryptographic hashing to create non-reversible, anonymous visitor IDs
/// </summary>
public class AnonymousIdService
{
    private readonly IConfiguration _configuration;

    public AnonymousIdService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Validates that the client-provided hash matches the expected hash
    /// This prevents clients from forging visitor IDs
    /// </summary>
    /// <param name="clientHash">Hash provided by the client</param>
    /// <param name="userAgent">User-Agent header from the request</param>
    /// <returns>True if the hash is valid, false otherwise</returns>
    public bool ValidateHash(string clientHash, string userAgent)
    {
        var expectedHash = GenerateHash(userAgent);
        return clientHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates a SHA-256 hash from the User-Agent and a server-side secret
    /// This ensures anonymity while allowing visitor recognition across sessions
    /// </summary>
    /// <param name="userAgent">User-Agent string from the browser</param>
    /// <returns>64-character hexadecimal hash</returns>
    public string GenerateHash(string userAgent)
    {
        var serverSecret = _configuration["Analytics:ServerSecret"]
            ?? throw new InvalidOperationException("Analytics:ServerSecret configuration is missing");

        var input = userAgent + serverSecret;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Extracts the User-Agent from the request headers
    /// </summary>
    public string? GetUserAgent(HttpContext httpContext)
    {
        return httpContext.Request.Headers.UserAgent.ToString();
    }
}
