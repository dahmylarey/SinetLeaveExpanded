using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SinetLeaveManagement.ViewComponents
{
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public NotificationBadgeViewComponent(UserManager<ApplicationUser> userManager,
                                              ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int count = 0;
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
                if (user != null)
                {
                    count = _context.Notifications.Count(n => n.UserId == user.Id && !n.IsRead);
                }
            }
            return View(count);
        }
    }
}
