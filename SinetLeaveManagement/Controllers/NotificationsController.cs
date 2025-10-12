using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinetLeaveManagement.Services;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Controllers
{
    // ======================
    // Notification Controller
    // ======================
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // ======================
        // Index - List all notifications
        // ======================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            return View(notifications);
        }

        // ======================
        // API endpoint to get unread count
        // ======================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            return Json(notifications.Count);
        }

        // ======================
        // Mark a notification as read
        // ======================
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }
    }
}
