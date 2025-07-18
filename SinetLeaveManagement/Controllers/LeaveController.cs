using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IMapper _mapper;

        public LeaveController(
            ILeaveService leaveService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext,
            IMapper mapper)
        {
            _leaveService = leaveService;
            _userManager = userManager;
            _emailService = emailService;
            _hubContext = hubContext;
            _mapper = mapper;
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

            var pagedList = query.OrderByDescending(l => l.RequestedAt).ToPagedList(page, 5);
            return View(pagedList);
        }

        // GET: /Leave/Create
        public IActionResult Create() => View();

        // POST: /Leave/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check your input.";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var leave = _mapper.Map<LeaveRequest>(model);
            leave.RequestingUserId = user.Id;
            leave.Status = "Pending";
            leave.RequestedAt = DateTime.UtcNow;

            await _leaveService.CreateLeaveRequestAsync(leave, user.Id);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "New leave request submitted.");
            await _leaveService.AddAuditLogAsync("Create", user.Id, leave.Id, "Created leave request");

            TempData["Success"] = "Leave request submitted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Leave/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool canEdit = leave.RequestingUserId == user.Id || await _userManager.IsInRoleAsync(user, "Admin");
            if (!canEdit) return Forbid();

            var model = _mapper.Map<LeaveRequestViewModel>(leave);
            return View(model);
        }

        // POST: /Leave/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeaveRequestViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            var updatedLeave = _mapper.Map<LeaveRequest>(model);

            await _leaveService.UpdateLeaveRequestAsync(id, updatedLeave);
            await _leaveService.AddAuditLogAsync("Edit", user.Id, id, "Edited leave request");

            TempData["Success"] = "Leave request updated!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Leave/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null) return NotFound();
            return View(leave);
        }

        // GET: /Leave/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool canDelete = leave.RequestingUserId == user.Id || await _userManager.IsInRoleAsync(user, "Admin");
            if (!canDelete) return Forbid();

            return View(leave);
        }

        // POST: /Leave/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            await _leaveService.DeleteLeaveRequestAsync(id);
            await _leaveService.AddAuditLogAsync("Delete", user.Id, id, "Deleted leave request");

            TempData["Success"] = "Leave request deleted!";
            return RedirectToAction(nameof(Index));
        }

        // Approve
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null)
            {
                TempData["Error"] = $"Leave #{id} not found.";
                return RedirectToAction(nameof(Index));
            }

            await _leaveService.ApproveLeaveRequestAsync(id, user.Id);
            await _leaveService.AddAuditLogAsync("Approve", user.Id, id, "Approved leave request");

            var requester = await _userManager.FindByIdAsync(leave.RequestingUserId);
            await _emailService.SendEmailAsync(requester.Email, "Leave Approved", "Your leave has been approved.");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Leave #{id} approved.");

            TempData["Success"] = $"Leave #{id} approved!";
            return RedirectToAction(nameof(Index));
        }

        // Reject
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null)
            {
                TempData["Error"] = $"Leave #{id} not found.";
                return RedirectToAction(nameof(Index));
            }

            await _leaveService.RejectLeaveRequestAsync(id, user.Id);
            await _leaveService.AddAuditLogAsync("Reject", user.Id, id, "Rejected leave request");

            var requester = await _userManager.FindByIdAsync(leave.RequestingUserId);
            await _emailService.SendEmailAsync(requester.Email, "Leave Rejected", "Your leave has been rejected.");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Leave #{id} rejected.");

            TempData["Success"] = $"Leave #{id} rejected!";
            return RedirectToAction(nameof(Index));
        }

        // Notifications
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            var list = await _leaveService.GetUnreadNotificationsAsync(user.Id);
            return View(list);
        }

        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            await _leaveService.MarkNotificationAsReadAsync(id, user.Id);
            return RedirectToAction("Notifications");
        }

        // Audit Logs
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _leaveService.GetAuditLogsAsync();
            return View(logs);
        }
    }
}
