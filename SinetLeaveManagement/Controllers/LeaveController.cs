using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // Needed for ToListAsync
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Hubs;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Models.ViewModels;
using SinetLeaveManagement.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.Extensions;

namespace SinetLeaveManagement.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly ILeaveService _leaveService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context; // For saving notifications

        public LeaveController(
            ILeaveService leaveService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext context)
        {
            _leaveService = leaveService;
            _userManager = userManager;
            _emailService = emailService;
            _hubContext = hubContext;
            _context = context;
        }

        // GET: /Leave
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            bool isAdminOrManager = await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Manager");

            var requests = await _leaveService.GetAllLeaveRequestsAsync();

            var query = isAdminOrManager
                ? requests.AsQueryable()
                : requests.Where(l => l.RequestingUserId == user.Id).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l =>
                    l.Status.Contains(search) ||
                    l.RequestingUser.FirstName.Contains(search) ||
                    l.RequestingUser.LastName.Contains(search));
            }

            var pagedList = query
                .OrderByDescending(l => l.RequestedAt)
                .ToPagedList(page, 5); // 5 per page

            return View(pagedList);
        }

        // GET: /Leave/Create
        public IActionResult Create(LeaveRequest model) => View();

        // POST: /Leave/Create
        [HttpPost]
        public async Task<IActionResult> Create(LeaveRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check your input.";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            var leave = new LeaveRequest
            {
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                RequestingUserId = user.Id,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            await _leaveService.CreateLeaveRequestAsync(leave);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "New leave request submitted.");

            TempData["Success"] = "Leave request submitted successfully!";
            return RedirectToAction(nameof(Index));
        }


        //Approve
        [Authorize(Roles = "Admin, Manager, HR")]
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null)
            {
                TempData["Error"] = $"Leave #{id} not found.";
                return RedirectToAction(nameof(Index));
            }

            await _leaveService.ApproveLeaveRequestAsync(id);

            var user = await _userManager.FindByIdAsync(leave.RequestingUserId);

            //Send email
            await _emailService.SendEmailAsync(user.Email, "Leave Approved", $"Your leave request #{id} has been approved.");

            //Add notification to DB
            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = $"Your leave request #{id} has been approved.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // SignalR notify all
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Leave #{id} approved.");

            TempData["Success"] = $"Leave #{id} approved!";
            return RedirectToAction(nameof(Index));
        }

        // Reject
        [Authorize(Roles = "Admin,Manager,HR")]
        public async Task<IActionResult> Reject(int id)
        {
            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null)
            {
                TempData["Error"] = $"Leave #{id} not found.";
                return RedirectToAction(nameof(Index));
            }

            await _leaveService.RejectLeaveRequestAsync(id);

            var user = await _userManager.FindByIdAsync(leave.RequestingUserId);

            //Send email
            await _emailService.SendEmailAsync(user.Email, "Leave Rejected", $"Your leave request #{id} has been rejected.");

            //Add notification to DB
            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = $"Your leave request #{id} has been rejected.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // ✅ SignalR notify all
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Leave #{id} rejected.");

            TempData["Success"] = $"Leave #{id} rejected!";
            return RedirectToAction(nameof(Index));
        }

        // List user notifications
        [Authorize]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // Mark single notification as read
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);

            if (notification != null && notification.UserId == user.Id)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }
    }
}
