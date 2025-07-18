using SinetLeaveManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
    public interface ILeaveService
    {
        Task<IEnumerable<LeaveRequest>> GetAllLeaveRequestsAsync();
        Task<LeaveRequest> GetLeaveRequestByIdAsync(int id);
        Task CreateLeaveRequestAsync(LeaveRequest request, string performedByUserId);
        Task UpdateLeaveRequestAsync(int id, LeaveRequest updatedRequest);
        Task DeleteLeaveRequestAsync(int id);
        Task ApproveLeaveRequestAsync(int id, string performedByUserId);
        Task RejectLeaveRequestAsync(int id, string performedByUserId);
        Task AddAuditLogAsync(string action, string performedByUserId, int? leaveRequestId, string details);
        Task<List<AuditLog>> GetAuditLogsAsync();

        // Notifications
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task MarkNotificationAsReadAsync(int notificationId, string userId);
    }
}
