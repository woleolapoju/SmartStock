using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SmartStock.Models;

namespace SmartStock.Services
{
    /// <summary>
    /// Sends email via SMTP using the "EmailSettings" configuration section.
    /// If no SMTP host is configured (e.g. local development), the message is
    /// written to the log instead of being sent, so password-reset flows keep
    /// working without requiring a mail server.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                // No SMTP server configured — log so the link is still usable in development.
                _logger.LogWarning(
                    "EmailSettings not configured. Email to {Email} was NOT sent.\nSubject: {Subject}\nBody:\n{Body}",
                    email, subject, htmlMessage);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(_settings.Username)
                    ? null
                    : new NetworkCredential(_settings.Username, _settings.Password)
            };

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}.", email);
                throw;
            }
        }
    }
}
