using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Services;

/// <summary>
/// Service for sending emails via SMTP using MailKit
/// </summary>
public class EmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Send an email using the configured SMTP settings
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="messageContent">Email body content (plain text)</param>
    /// <param name="isHtml">Whether the message content is HTML (default: false)</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string messageContent, bool isHtml = false)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.Username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = messageContent;
            }
            else
            {
                bodyBuilder.TextBody = messageContent;
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect to the SMTP server
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port,
                _smtpSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            // Authenticate
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);

            // Send the email
            await client.SendAsync(message);

            // Disconnect
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {ToEmail} with subject: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject: {Subject}", toEmail, subject);
            return false;
        }
    }

    /// <summary>
    /// Send a contact form submission email to the configured recipient
    /// </summary>
    /// <param name="senderName">Name of the person submitting the form</param>
    /// <param name="senderEmail">Email of the person submitting the form</param>
    /// <param name="message">Message content from the form</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    public async Task<bool> SendContactFormEmailAsync(string senderName, string senderEmail, string message)
    {
        var subject = $"New Contact Form Submission from {senderName}";

        var messageContent = $@"You have received a new contact form submission:

From: {senderName}
Email: {senderEmail}

Message:
{message}

---
This email was sent from your portfolio contact form.
Reply directly to this email to respond to {senderName}.";

        return await SendEmailAsync(_smtpSettings.RecipientEmail, subject, messageContent);
    }

    /// <summary>
    /// Send a confirmation email to the person who submitted the contact form
    /// </summary>
    /// <param name="recipientName">Name of the person who submitted the form</param>
    /// <param name="recipientEmail">Email of the person who submitted the form</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    public async Task<bool> SendContactConfirmationEmailAsync(string recipientName, string recipientEmail)
    {
        var subject = "Vielen Dank für Ihre Nachricht / Thank you for your message";

        var messageContent = $@"
Hallo {recipientName},

vielen Dank für Ihre Nachricht über mein Portfolio-Kontaktformular.
Ich habe Ihre Anfrage erhalten und werde mich so schnell wie möglich bei Ihnen melden.

Mit freundlichen Grüßen
Jan Hülsbrink

---
Hello {recipientName},

thank you for reaching out through my portfolio contact form.
I have received your message and will get back to you as soon as possible.

Best regards,
Jan Hülsbrink

---
Dies ist eine automatische Bestätigungs-E-Mail. Bitte antworten Sie nicht auf diese Nachricht.
This is an automated confirmation email. Please do not reply to this message.
";

    return await SendEmailAsync(recipientEmail, subject, messageContent);
}
}
