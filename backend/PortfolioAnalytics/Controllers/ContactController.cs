using Microsoft.AspNetCore.Mvc;
using PortfolioAnalytics.DTOs;
using PortfolioAnalytics.Services;

namespace PortfolioAnalytics.Controllers;

/// <summary>
/// Controller for handling contact form submissions
/// </summary>
[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        EmailService emailService,
        ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a contact form and send email notification
    /// </summary>
    /// <param name="dto">Contact form data</param>
    /// <returns>Response indicating success or failure</returns>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitContactForm([FromBody] ContactFormDto dto)
    {
        // Validate model state
        if (!ModelState.IsValid)
        {
            return BadRequest(new ContactFormResponseDto
            {
                Success = false,
                Message = "Invalid form data. Please check your input and try again."
            });
        }

        // Honeypot spam detection
        if (!string.IsNullOrWhiteSpace(dto.HoneyPot))
        {
            _logger.LogWarning("Potential spam detected: Honeypot field was filled");

            // Return success to not reveal spam detection to bots
            return Ok(new ContactFormResponseDto
            {
                Success = true,
                Message = "Thank you for your message. We will get back to you soon."
            });
        }

        try
        {
            // Send contact form email to recipient
            var emailSent = await _emailService.SendContactFormEmailAsync(
                dto.Name,
                dto.Email,
                dto.Message
            );

            if (!emailSent)
            {
                _logger.LogError("Failed to send contact form email from {Name} ({Email})", dto.Name, dto.Email);

                return StatusCode(500, new ContactFormResponseDto
                {
                    Success = false,
                    Message = "An error occurred while sending your message. Please try again later."
                });
            }

            // Optionally send confirmation email to sender
            if (dto.SendConfirmation)
            {
                var confirmationSent = await _emailService.SendContactConfirmationEmailAsync(
                    dto.Name,
                    dto.Email
                );

                if (!confirmationSent)
                {
                    _logger.LogWarning("Failed to send confirmation email to {Email}", dto.Email);
                    // Don't fail the request if only confirmation fails
                }
            }

            _logger.LogInformation("Contact form submitted successfully from {Name} ({Email})", dto.Name, dto.Email);

            return Ok(new ContactFormResponseDto
            {
                Success = true,
                Message = "Thank you for your message. I will get back to you as soon as possible!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing contact form from {Name} ({Email})", dto.Name, dto.Email);

            return StatusCode(500, new ContactFormResponseDto
            {
                Success = false,
                Message = "An unexpected error occurred. Please try again later."
            });
        }
    }

    /// <summary>
    /// Test endpoint to verify SMTP configuration (should be protected in production)
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailDto dto)
    {
        try
        {
            _logger.LogInformation("Attempting to send test email to {ToEmail}", dto.ToEmail);

            var success = await _emailService.SendEmailAsync(
                dto.ToEmail,
                "Test Email from Portfolio",
                "This is a test email to verify your SMTP configuration is working correctly."
            );

            if (success)
            {
                _logger.LogInformation("Test email sent successfully to {ToEmail}", dto.ToEmail);
                return Ok(new { message = "Test email sent successfully" });
            }
            else
            {
                _logger.LogError("Failed to send test email to {ToEmail} - SendEmailAsync returned false", dto.ToEmail);
                return StatusCode(500, new { message = "Failed to send test email. Check server logs for details." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending test email to {ToEmail}. Error: {ErrorMessage}, StackTrace: {StackTrace}",
                dto.ToEmail, ex.Message, ex.StackTrace);
            return StatusCode(500, new {
                message = $"Error: {ex.Message}",
                type = ex.GetType().Name,
                details = ex.InnerException?.Message
            });
        }
    }
}

/// <summary>
/// DTO for testing email functionality
/// </summary>
public class TestEmailDto
{
    public string ToEmail { get; set; } = string.Empty;
}
