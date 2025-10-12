using SinetLeaveManagement.Models;

namespace SinetLeaveManagement.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task AddNotificationAsync(string userId, string message);
        Task MarkAsReadAsync(int notificationId);
    }
}
