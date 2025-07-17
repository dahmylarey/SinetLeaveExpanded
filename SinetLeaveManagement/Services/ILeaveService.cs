using SinetLeaveManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
    public interface ILeaveService
    {
        Task<IEnumerable<LeaveRequest>> GetAllLeaveRequestsAsync();
        Task<LeaveRequest> GetLeaveRequestByIdAsync(int id);
        Task CreateLeaveRequestAsync(LeaveRequest request);
        Task ApproveLeaveRequestAsync(int id);
        Task RejectLeaveRequestAsync(int id);
    }
}
