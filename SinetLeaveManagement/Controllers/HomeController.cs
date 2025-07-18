using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Models.ViewModels;
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

        int count = 0;
        if (user != null)
        {
            count = _context.Notifications.Count(n => n.UserId == user.Id && !n.IsRead);
        }

        var model = new HomeIndexViewModel
        {
            UnreadNotificationCount = count
        };

        return View(model);
    }

    public IActionResult Privacy() => View();
}
