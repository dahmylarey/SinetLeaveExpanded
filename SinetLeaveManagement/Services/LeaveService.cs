using Microsoft.EntityFrameworkCore;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task CreateLeaveRequestAsync(LeaveRequest request, string performedByUserId)
        {
            _context.LeaveRequests.Add(request);

            var managers = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                    ur.RoleId == _context.Roles.First(r => r.Name == "Manager").Id))
                .ToListAsync();

            foreach (var manager in managers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = manager.Id,
                    Message = $"New leave request submitted by user ID {request.RequestingUserId}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await AddAuditLogAsync("Create", performedByUserId, request.Id, "Created leave request");
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLeaveRequestAsync(int id, LeaveRequest updatedRequest)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                leave.StartDate = updatedRequest.StartDate;
                leave.EndDate = updatedRequest.EndDate;
                leave.Reason = updatedRequest.Reason;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteLeaveRequestAsync(int id)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                _context.LeaveRequests.Remove(leave);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ApproveLeaveRequestAsync(int id, string performedByUserId)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "Approved";
                _context.Notifications.Add(new Notification
                {
                    UserId = leave.RequestingUserId,
                    Message = $"Your leave request #{id} was approved.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
                await AddAuditLogAsync("Approve", performedByUserId, id, "Approved leave request");
                await _context.SaveChangesAsync();
            }
        }

        public async Task RejectLeaveRequestAsync(int id, string performedByUserId)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "Rejected";
                _context.Notifications.Add(new Notification
                {
                    UserId = leave.RequestingUserId,
                    Message = $"Your leave request #{id} was rejected.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
                await AddAuditLogAsync("Reject", performedByUserId, id, "Rejected leave request");
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddAuditLogAsync(string action, string performedByUserId, int? leaveRequestId, string details)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                PerformedBy = performedByUserId,
                LeaveRequestId = leaveRequestId,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync()
        {
            return await _context.AuditLogs
                .Include(a => a.PerformedBy)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkNotificationAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
