using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Models.ViewModels;
using SinetLeaveManagement.Services;
using System.Linq;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    //private readonly INotificationService _notificationService;



    public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Get the current logged-in user
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // --- Calculate leave usage ---
        var annualUsed = await _context.LeaveRequests
            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Annual Leave")
            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

        var sickUsed = await _context.LeaveRequests
            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Sick Leave")
            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

        var personalUsed = await _context.LeaveRequests
            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Personal Leave")
            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

        // --- Hardcoded totals for now (replace if you store them in DB) ---
        int annualTotal = 20;
        int sickTotal = 10;
        int personalTotal = 5;

        // --- Recent activities ---
        var recentActivities = await _context.LeaveRequests
            .Where(x => x.RequestingUserId == user.Id)
            .OrderByDescending(x => x.RequestedAt)
            .Take(5)
            .Select(x => new RecentActivityItem
            {
                LeaveType = x.LeaveType != null ? x.LeaveType.Name : "Unknown",
                DateApplied = x.RequestedAt,
                Duration = $"{x.StartDate:dd MMM} - {x.EndDate:dd MMM}",
                Status = x.Status
            })
            .ToListAsync();

        // --- Build ViewModel ---
        var model = new HomeIndexViewModel
        {
            AnnualLeaveUsed = annualUsed,
            AnnualLeaveTotal = annualTotal,

            SickLeaveUsed = sickUsed,
            SickLeaveTotal = sickTotal,

            PersonalLeaveUsed = personalUsed,
            PersonalLeaveTotal = personalTotal,

            RecentActivities = recentActivities
        };

        return View(model);
    }





    //public IActionResult Index()
    //{
    //    // Sample recent leave requests (replace with real data later)
    //    var recentRequests = new List<LeaveRequest>
    //{
    //    new LeaveRequest { Id = 1, StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today.AddDays(-8), Status = "Approved", Reason = "Vacation" },
    //    new LeaveRequest { Id = 2, StartDate = DateTime.Today.AddDays(-5), EndDate = DateTime.Today.AddDays(-4), Status = "Pending", Reason = "Sick Leave" },
    //    new LeaveRequest { Id = 3, StartDate = DateTime.Today.AddDays(-2), EndDate = DateTime.Today, Status = "Rejected", Reason = "Personal" }
    //};

    //    var model = new HomeIndexViewModel
    //    {
    //        UnreadNotificationCount = 1, // Dummy data

    //        AnnualLeaveUsed = 8,
    //        AnnualLeaveTotal = 20,

    //        SickLeaveUsed = 2,
    //        SickLeaveTotal = 12,

    //        PersonalLeaveUsed = 1,
    //        PersonalLeaveTotal = 5,

    //        RecentLeaveRequests = recentRequests
    //    };

    //    return View(model);
    //}

    public IActionResult LeaveBalance()
    {
        var model = new HomeIndexViewModel
        {
            AnnualLeaveUsed = 8,
            AnnualLeaveTotal = 25,
            SickLeaveUsed = 2,
            SickLeaveTotal = 12,
            PersonalLeaveUsed = 1,
            PersonalLeaveTotal = 5
        };
        return View(model);
    }

    public IActionResult LeaveHistory()
    {
        var leaveRequests = new List<LeaveRequest>
    {
        new LeaveRequest
        {
            StartDate = DateTime.Today.AddDays(-10),
            EndDate = DateTime.Today.AddDays(-8),
            Reason = "Annual Vacation",
            Status = "Approved"
        },
        new LeaveRequest
        {
            StartDate = DateTime.Today.AddDays(-5),
            EndDate = DateTime.Today.AddDays(-3),
            Reason = "Medical Checkup",
            Status = "Rejected"
        }
    };
        return View(leaveRequests);
    }

    public IActionResult PendingUpdates()
    {
        var pendingRequests = new List<LeaveRequest>
    {
        new LeaveRequest
        {
            StartDate = DateTime.Today.AddDays(2),
            EndDate = DateTime.Today.AddDays(5),
            Reason = "Family Event",
            Status = "Pending"
        }
    };
        return View(pendingRequests);
    }


    public IActionResult Privacy() => View();
}
