using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using System.Linq;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        //  Count pending requests
        var pendingRequestsCount = await _context.LeaveRequests
            .CountAsync(x => x.RequestingUserId == user.Id && x.Status == "Pending");

        // Calculate used days by type (null-safe)
        int annualUsed = await _context.LeaveRequests
            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Annual Leave")
            .SumAsync(x => (int?)EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1) ?? 0;

        int sickUsed = await _context.LeaveRequests
            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Sick Leave")
            .SumAsync(x => (int?)EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1) ?? 0;

        int personalUsed = await _context.LeaveRequests
            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Personal Leave")
            .SumAsync(x => (int?)EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1) ?? 0;

        // Static totals (can later come from DB config)
        const int annualTotal = 20;
        const int sickTotal = 10;
        const int personalTotal = 5;

        //Fetch last 5 activities
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

        //Pass everything to view
        var model = new HomeIndexViewModel
        {
            PendingRequestsCount = pendingRequestsCount,
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

    // Leave Balance
    public IActionResult LeaveBalance()
    { 
        var model = new HomeIndexViewModel
        { AnnualLeaveUsed = 8, AnnualLeaveTotal = 20, SickLeaveUsed = 2, SickLeaveTotal = 10, PersonalLeaveUsed = 1, PersonalLeaveTotal = 5 
        };

        return View(model); 
    
    }
    //pending updates
    public IActionResult PendingUpdates()
    { 
        var pendingRequests = new List<LeaveRequest>
        { 
            new LeaveRequest { StartDate = DateTime.Today.AddDays(2), EndDate = DateTime.Today.AddDays(5), Reason = "Family Event", Status = "Pending" }
        }; return View(pendingRequests);
    }
    // Leave History
    public IActionResult LeaveHistory()
    {
        var leaveRequests = new List<LeaveRequest>
        { 
            new LeaveRequest { StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today.AddDays(-8), Reason = "Annual Vacation", Status = "Approved" },
            new LeaveRequest { StartDate = DateTime.Today.AddDays(-5), EndDate = DateTime.Today.AddDays(-3), Reason = "Medical Checkup", Status = "Rejected" }
        };
        return View(leaveRequests);
    }
    // Privacy
    public IActionResult Privacy() => View();

    //[Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        var model = new EditProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ExistingPicturePath = user.ProfilePicturePath
        };
        return View(model);
    }


    [HttpPost]
    public async Task<IActionResult> Profile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        // Update basic info
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;

        // ======================
        // HANDLE PROFILE PICTURE UPLOAD
        // ======================
        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            // Ensure the directory exists
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/profilepics");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ProfilePicture.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfilePicture.CopyToAsync(stream);
            }

            // Delete old file (if exists)
            if (!string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicturePath);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            // Save relative path (for display)
            user.ProfilePicturePath = $"profilepics/{fileName}";
        }

        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }

    // Access Denied
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

}


//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using SinetLeaveManagement.Data;
//using SinetLeaveManagement.Models;
//using SinetLeaveManagement.Models.ViewModels;
//using System.Linq;
//using System.Threading.Tasks;

//public class HomeController : Controller
//{
//    private readonly UserManager<ApplicationUser> _userManager;
//    private readonly ApplicationDbContext _context;

//    public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
//    {
//        _userManager = userManager;
//        _context = context;
//    }

//    // Home dashboard for logged-in user
//    public async Task<IActionResult> Index()
//    {
//        // Get the currently logged-in user
//        var user = await _userManager.GetUserAsync(User);
//        if (user == null)
//        {
//            return RedirectToAction("Login", "Account");
//        }

//        // Count pending leave requests (for dashboard tile)
//        var pendingRequestsCount = await _context.LeaveRequests
//            .CountAsync(x => x.RequestingUserId == user.Id && x.Status == "Pending");

//        // --- Calculate leave usage per type ---
//        var annualUsed = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Annual Leave")
//            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

//        var sickUsed = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Sick Leave")
//            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

//        var personalUsed = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Personal Leave")
//            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

//        // --- Define leave totals (these can later come from DB) ---
//        int annualTotal = 20;
//        int sickTotal = 10;
//        int personalTotal = 5;

//        // --- Fetch the 5 most recent activities ---
//        var recentActivities = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id)
//            .OrderByDescending(x => x.RequestedAt)
//            .Take(5)
//            .Select(x => new RecentActivityItem
//            {
//                LeaveType = x.LeaveType != null ? x.LeaveType.Name : "Unknown",
//                DateApplied = x.RequestedAt,
//                Duration = $"{x.StartDate:dd MMM} - {x.EndDate:dd MMM}",
//                Status = x.Status
//            })
//            .ToListAsync();

//        // --- Build the view model for the Home Dashboard ---
//        var model = new HomeIndexViewModel
//        {
//            AnnualLeaveUsed = annualUsed,
//            AnnualLeaveTotal = annualTotal,

//            SickLeaveUsed = sickUsed,
//            SickLeaveTotal = sickTotal,

//            PersonalLeaveUsed = personalUsed,
//            PersonalLeaveTotal = personalTotal,

//            PendingRequestsCount = pendingRequestsCount,
//            RecentActivities = recentActivities
//        };

//        return View(model);
//    }

//    public IActionResult Privacy() => View();
//}




//public class HomeController : Controller
//{
//    private readonly UserManager<ApplicationUser> _userManager;
//    private readonly ApplicationDbContext _context;
//    //private readonly INotificationService _notificationService;



//    public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
//    {
//        _userManager = userManager;
//        _context = context;
//    }

//    public async Task<IActionResult> Index()
//    {
//        // Get the current logged-in user
//        var user = await _userManager.GetUserAsync(User);
//        if (user == null)
//        {
//            return RedirectToAction("Login", "Account");
//        }

//        // --- Calculate leave usage ---
//        var annualUsed = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Annual Leave")
//            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

//        var sickUsed = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Sick Leave")
//            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

//        var personalUsed = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id && x.LeaveType != null && x.LeaveType.Name == "Personal Leave")
//            .SumAsync(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1);

//        // --- Hardcoded totals for now (replace if you store them in DB) ---
//        int annualTotal = 20;
//        int sickTotal = 10;
//        int personalTotal = 5;

//        // --- Recent activities ---
//        var recentActivities = await _context.LeaveRequests
//            .Where(x => x.RequestingUserId == user.Id)
//            .OrderByDescending(x => x.RequestedAt)
//            .Take(5)
//            .Select(x => new RecentActivityItem
//            {
//                LeaveType = x.LeaveType != null ? x.LeaveType.Name : "Unknown",
//                DateApplied = x.RequestedAt,
//                Duration = $"{x.StartDate:dd MMM} - {x.EndDate:dd MMM}",
//                Status = x.Status
//            })
//            .ToListAsync();

//        // --- Build ViewModel ---
//        var model = new HomeIndexViewModel
//        {
//            AnnualLeaveUsed = annualUsed,
//            AnnualLeaveTotal = annualTotal,

//            SickLeaveUsed = sickUsed,
//            SickLeaveTotal = sickTotal,

//            PersonalLeaveUsed = personalUsed,
//            PersonalLeaveTotal = personalTotal,

//            RecentActivities = recentActivities
//        };

//        return View(model);
//    }





//    //public IActionResult Index()
//    //{
//    //    // Sample recent leave requests (replace with real data later)
//    //    var recentRequests = new List<LeaveRequest>
//    //{
//    //    new LeaveRequest { Id = 1, StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today.AddDays(-8), Status = "Approved", Reason = "Vacation" },
//    //    new LeaveRequest { Id = 2, StartDate = DateTime.Today.AddDays(-5), EndDate = DateTime.Today.AddDays(-4), Status = "Pending", Reason = "Sick Leave" },
//    //    new LeaveRequest { Id = 3, StartDate = DateTime.Today.AddDays(-2), EndDate = DateTime.Today, Status = "Rejected", Reason = "Personal" }
//    //};

//    //    var model = new HomeIndexViewModel
//    //    {
//    //        UnreadNotificationCount = 1, // Dummy data

//    //        AnnualLeaveUsed = 8,
//    //        AnnualLeaveTotal = 20,

//    //        SickLeaveUsed = 2,
//    //        SickLeaveTotal = 12,

//    //        PersonalLeaveUsed = 1,
//    //        PersonalLeaveTotal = 5,

//    //        RecentLeaveRequests = recentRequests
//    //    };

//    //    return View(model);
//    //}

//    public IActionResult LeaveBalance()
//    {
//        var model = new HomeIndexViewModel
//        {
//            AnnualLeaveUsed = 8,
//            AnnualLeaveTotal = 25,
//            SickLeaveUsed = 2,
//            SickLeaveTotal = 12,
//            PersonalLeaveUsed = 1,
//            PersonalLeaveTotal = 5
//        };
//        return View(model);
//    }

//    public IActionResult LeaveHistory()
//    {
//        var leaveRequests = new List<LeaveRequest>
//    {
//        new LeaveRequest
//        {
//            StartDate = DateTime.Today.AddDays(-10),
//            EndDate = DateTime.Today.AddDays(-8),
//            Reason = "Annual Vacation",
//            Status = "Approved"
//        },
//        new LeaveRequest
//        {
//            StartDate = DateTime.Today.AddDays(-5),
//            EndDate = DateTime.Today.AddDays(-3),
//            Reason = "Medical Checkup",
//            Status = "Rejected"
//        }
//    };
//        return View(leaveRequests);
//    }

//    public IActionResult PendingUpdates()
//    {
//        var pendingRequests = new List<LeaveRequest>
//    {
//        new LeaveRequest
//        {
//            StartDate = DateTime.Today.AddDays(2),
//            EndDate = DateTime.Today.AddDays(5),
//            Reason = "Family Event",
//            Status = "Pending"
//        }
//    };
//        return View(pendingRequests);
//    }


//    public IActionResult Privacy() => View();
//}
