using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtp = new SmtpClient("smtp.yourserver.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("your@email.com", "password"),
                EnableSsl = true
            };

            var message = new MailMessage("your@email.com", toEmail, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(message);
        }
    }
}
