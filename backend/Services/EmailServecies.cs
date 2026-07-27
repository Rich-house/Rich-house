using MailKit.Security;
using Marketify.Settings;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace Marketify.Services
{
    public class EmailServecies(IOptions<MailSettings> mailsettings, ILogger<EmailServecies> logger) : IEmailSender
    {
        private readonly MailSettings _mailSettings = mailsettings.Value;
        private readonly ILogger<EmailServecies> _logger = logger;

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage
            {
                Sender = MailboxAddress.Parse(_mailSettings.Mail),
                Subject = subject,
            };

            message.To.Add(MailboxAddress.Parse(email));

            var builder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };

            _logger.LogInformation("Sending email to {Email}.", email);
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
