using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _settings;

        public EmailService(IOptions<MailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            using (var smtp = new SmtpClient(_settings.SmtpServer, _settings.Port))
            {
                smtp.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
                smtp.EnableSsl = true;

                var mail = new MailMessage
                {
                    From = new MailAddress(_settings.From, "Sinet Leave Management"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                await smtp.SendMailAsync(mail);
            }
        }
    }
}
