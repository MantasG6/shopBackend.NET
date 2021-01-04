using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MimeKit;
using shop.Models;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace shop.Services
{
    public class Mailer : IMailer
    {
        private readonly SmtpSettings _smtpSettings;

        public Mailer(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            message.Subject = subject;
            string[] emails = _smtpSettings.Receivers;
            foreach (string email in emails)
            {
                message.To.Add(MailboxAddress.Parse(email));
            }
            message.Body = new TextPart("html") { Text = body };
                
            using (SmtpClient client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
