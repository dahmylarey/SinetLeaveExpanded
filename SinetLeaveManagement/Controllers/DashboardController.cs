using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinetLeaveManagement.Data;
using System.Linq;

namespace SinetLeaveManagement.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetStats()
        {
            var total = _context.LeaveRequests.Count();
            var approved = _context.LeaveRequests.Count(l => l.Status == "Approved");
            var pending = _context.LeaveRequests.Count(l => l.Status == "Pending");
            var rejected = _context.LeaveRequests.Count(l => l.Status == "Rejected");

            return Json(new { total, approved, pending, rejected });
        }
    }
}
