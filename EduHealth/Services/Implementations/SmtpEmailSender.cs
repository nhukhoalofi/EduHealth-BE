using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using EduHealth.Services.Interfaces;

namespace EduHealth.Services.Implementations
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var host = _configuration["Mail:Smtp:Host"];
            var portStr = _configuration["Mail:Smtp:Port"];
            var user = _configuration["Mail:Smtp:User"];
            var pass = _configuration["Mail:Smtp:Pass"];
            var fromEmail = _configuration["Mail:From:Email"];
            var fromName = _configuration["Mail:From:Name"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portStr) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("SMTP mail settings are not configured.");
            }

            if (!int.TryParse(portStr, out var port))
            {
                throw new InvalidOperationException("Mail:Smtp:Port is invalid.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName ?? string.Empty, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Typical SMTP servers: STARTTLS on 587.
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
            await client.AuthenticateAsync(user, pass, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
