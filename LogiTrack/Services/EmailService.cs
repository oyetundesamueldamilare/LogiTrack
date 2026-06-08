using LogiTrack.Interfaces;
using LogiTrack.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LogiTrack.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        // Injecting settings using IOptions snapshot/monitor
        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();

            // Set From and To addresses
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));

            message.Subject = subject;

            // Build the email body (using TextPart or BodyBuilder for HTML)
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body // Accepts HTML formatting for cleaner reset links
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Connect to the SMTP server (StartTls is standard for port 587)
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);

                // Authenticate with the server credentials
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                // Send the message
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                // In production, log this exception using ILogger
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
            finally
            {
                // Cleanly disconnect from the server
                await client.DisconnectAsync(true);
            }
        }
    }
}