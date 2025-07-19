using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class LeaveService : ILeaveService
{
    private readonly ApplicationDbContext _context;

    public LeaveService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Export to Excel
    public async Task<MemoryStream> ExportToExcelAsync(IEnumerable<LeaveRequest> requests)
    {
        var stream = new MemoryStream();

        using (var package = new OfficeOpenXml.ExcelPackage())
        {
            var ws = package.Workbook.Worksheets.Add("LeaveRequests");
            ws.Cells.LoadFromCollection(requests.Select(r => new
            {
                r.Id,
                r.RequestingUser.FirstName,
                r.RequestingUser.LastName,
                r.StartDate,
                r.EndDate,
                r.Reason,
                r.Status,
                RequestedAt = r.RequestedAt.ToLocalTime()
            }), true);

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            ws.Row(1).Style.Font.Bold = true;

            await package.SaveAsAsync(stream);
        }
        stream.Position = 0;
        return stream;
    }

    // Export to PDF
    public async Task<byte[]> ExportToPdfAsync(IEnumerable<LeaveRequest> requests)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Leave Requests").FontSize(20).SemiBold().AlignCenter();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1); // Id
                        columns.RelativeColumn(2); // Name
                        columns.RelativeColumn(2); // StartDate
                        columns.RelativeColumn(2); // EndDate
                        columns.RelativeColumn(2); // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ID").Bold();
                        header.Cell().Text("Employee");
                        header.Cell().Text("Start Date");
                        header.Cell().Text("End Date");
                        header.Cell().Text("Status");
                    });

                    foreach (var r in requests)
                    {
                        table.Cell().Text(r.Id.ToString());
                        table.Cell().Text($"{r.RequestingUser.FirstName} {r.RequestingUser.LastName}");
                        table.Cell().Text(r.StartDate.ToShortDateString());
                        table.Cell().Text(r.EndDate.ToShortDateString());
                        table.Cell().Text(r.Status);
                    }
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated on ");
                    x.Span($"{DateTime.Now}");
                });
            });
        });

        return pdf.GeneratePdf();
    }




    // ... other methods (Create, Update, Delete, etc.) ...

    public async Task<IEnumerable<LeaveRequest>> GetAllLeaveRequestsAsync()
    {
        return await _context.LeaveRequests.Include(l => l.RequestingUser).ToListAsync();
    }

    public async Task<LeaveRequest> GetLeaveRequestByIdAsync(int id)
    {
        return await _context.LeaveRequests.Include(l => l.RequestingUser).FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leave, string performedByUserId)
    {
        _context.LeaveRequests.Add(leave);
        await _context.SaveChangesAsync(); // leave.Id now has value

        await AddAuditLogAsync("Create", performedByUserId, leave.Id, "Created leave request");
        return leave;
    }

    public async Task UpdateLeaveRequestAsync(int id, LeaveRequest updated, string performedByUserId)
    {
        var existing = await _context.LeaveRequests.FindAsync(id);
        if (existing == null) throw new Exception("Leave not found");

        existing.StartDate = updated.StartDate;
        existing.EndDate = updated.EndDate;
        existing.Reason = updated.Reason;
        existing.Status = updated.Status;

        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Edit", performedByUserId, id, "Edited leave request");
    }

    public async Task DeleteLeaveRequestAsync(int id, string performedByUserId)
    {
        var leave = await _context.LeaveRequests.FindAsync(id);
        if (leave == null) throw new Exception("Leave not found");

        _context.LeaveRequests.Remove(leave);
        await _context.SaveChangesAsync();

        await AddAuditLogAsync("Delete", performedByUserId, id, "Deleted leave request");
    }


    public async Task ApproveLeaveRequestAsync(int id, string performedByUserId)
    {
        var leave = await _context.LeaveRequests.FindAsync(id);
        if (leave != null)
        {
            leave.Status = "Approved";
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
            await AddAuditLogAsync("Reject", performedByUserId, id, "Rejected leave request");
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync()
    {
        return await _context.AuditLogs.Include(a => a.PerformedByUser).OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task AddAuditLogAsync(string action, string performedByUserId, int? leaveRequestId, string details)
    {
        int? validLeaveRequestId = null;
        if (leaveRequestId.HasValue)
        {
            var exists = await _context.LeaveRequests.AnyAsync(l => l.Id == leaveRequestId.Value);
            if (exists)
                validLeaveRequestId = leaveRequestId;
            else
                details += $" (Warning: LeaveRequestId {leaveRequestId} not found)";
        }

        var log = new AuditLog
        {
            Action = action,
            PerformedByUserId = performedByUserId,
            LeaveRequestId = validLeaveRequestId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
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
