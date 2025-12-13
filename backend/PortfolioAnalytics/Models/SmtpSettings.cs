namespace PortfolioAnalytics.Models;

/// <summary>
/// Configuration settings for SMTP email sending
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// SMTP server host address (e.g., smtp.office365.com, smtp.gmail.com)
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 587 for TLS/STARTTLS, 465 for SSL)
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Email address used for authentication and as sender
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password or app-specific password for SMTP authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the sender (e.g., "Portfolio Contact Form")
    /// </summary>
    public string SenderName { get; set; } = "Portfolio Contact Form";

    /// <summary>
    /// Email address to receive contact form submissions
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS encryption
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}
