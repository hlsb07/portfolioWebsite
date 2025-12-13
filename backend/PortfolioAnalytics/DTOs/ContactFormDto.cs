using System.ComponentModel.DataAnnotations;

namespace PortfolioAnalytics.DTOs;

/// <summary>
/// Data transfer object for contact form submissions
/// </summary>
public class ContactFormDto
{
    /// <summary>
    /// Name of the person submitting the contact form
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the person submitting the contact form
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Message content from the contact form
    /// </summary>
    [Required(ErrorMessage = "Message is required")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 5000 characters")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Honeypot field to detect spam bots (should remain empty)
    /// </summary>
    public string? HoneyPot { get; set; }

    /// <summary>
    /// Whether to send a confirmation email to the sender
    /// </summary>
    public bool SendConfirmation { get; set; } = true;
}

/// <summary>
/// Response DTO for contact form submission
/// </summary>
public class ContactFormResponseDto
{
    /// <summary>
    /// Whether the submission was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the submission
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
