using Microsoft.EntityFrameworkCore;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
    public class LeaveService : ILeaveService
    {
        private readonly ApplicationDbContext _context;

        public LeaveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LeaveRequest>> GetAllLeaveRequestsAsync()
        {
            return await _context.LeaveRequests
                .Include(l => l.RequestingUser)
                .ToListAsync();
        }

        public async Task<LeaveRequest> GetLeaveRequestByIdAsync(int id)
        {
            return await _context.LeaveRequests
                .Include(l => l.RequestingUser)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task CreateLeaveRequestAsync(LeaveRequest request)
        {
            if (request != null)
            {
                if (request.RequestedAt == default) request.RequestedAt = System.DateTime.UtcNow;
                _context.LeaveRequests.Add(request);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ApproveLeaveRequestAsync(int id)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "Approved";
                await _context.SaveChangesAsync();
            }
        }

        public async Task RejectLeaveRequestAsync(int id)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "Rejected";
                await _context.SaveChangesAsync();
            }
        }
    }
}
