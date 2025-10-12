using Microsoft.AspNetCore.Mvc;
using SinetLeaveManagement.Services;

namespace SinetLeaveManagement.Controllers
{
    public class TestemailController : Controller
    {
        private readonly IEmailService _emailService;

        public TestemailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendEmailAsync("yourpersonalemail@example.com", "Test Email", "<h3>This is a test from Sinet Leave App ✅</h3>");
            return Content("Email sent successfully!");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
