using SinetLeaveManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
   
    public interface ILeaveService
    {
        Task<IEnumerable<LeaveRequest>> GetAllLeaveRequestsAsync();
        Task<LeaveRequest> GetLeaveRequestByIdAsync(int id);
        Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leave, string performedByUserId);
        Task UpdateLeaveRequestAsync(int id, LeaveRequest updated, string performedByUserId);
        Task DeleteLeaveRequestAsync(int id, string performedByUserId);
        Task ApproveLeaveRequestAsync(int id, string performedByUserId);
        Task RejectLeaveRequestAsync(int id, string performedByUserId);

        Task<List<AuditLog>> GetAuditLogsAsync();
        Task AddAuditLogAsync(string action, string performedByUserId, int? leaveRequestId, string note);

        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task MarkNotificationAsReadAsync(int notificationId, string userId);

        Task<MemoryStream> ExportToExcelAsync(IEnumerable<LeaveRequest> requests);
        Task<byte[]> ExportToPdfAsync(IEnumerable<LeaveRequest> requests);
    }

}
