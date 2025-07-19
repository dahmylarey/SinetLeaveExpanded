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

            var savedLeave = await _leaveService.CreateLeaveRequestAsync(leave, user.Id);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "New leave request submitted.");


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

            await _leaveService.UpdateLeaveRequestAsync(id, updatedLeave, user.Id);


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
            await _leaveService.DeleteLeaveRequestAsync(id, user.Id);
            
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

        // Export to Excel
        //[Authorize]

        //public async Task<IActionResult> ExportToPdf(int id)
        //{
        //    var leave = await _leaveService.GetLeaveRequestByIdAsync(id);

        //    if (leave == null) return NotFound();

        //    using (var stream = new MemoryStream())
        //    {
        //        var document = new PdfSharpCore.Pdf.PdfDocument();
        //        var page = document.AddPage();
        //        var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
        //        var font = new PdfSharpCore.Drawing.XFont("Verdana", 14);

        //        gfx.DrawString($"Leave Request #{leave.Id}", font, PdfSharpCore.Drawing.XBrushes.Black, new PdfSharpCore.Drawing.XRect(0, 0, page.Width, 50), PdfSharpCore.Drawing.XStringFormats.TopCenter);

        //        var text = $"Name: {leave.RequestingUser.FirstName} {leave.RequestingUser.LastName}\n" +
        //                   $"Dates: {leave.StartDate:d} to {leave.EndDate:d}\n" +
        //                   $"Status: {leave.Status}\n" +
        //                   $"Reason: {leave.Reason}";

        //        gfx.DrawString(text, new PdfSharpCore.Drawing.XFont("Verdana", 12), PdfSharpCore.Drawing.XBrushes.Black, new PdfSharpCore.Drawing.XRect(40, 60, page.Width - 80, page.Height - 60));

        //        document.Save(stream, false);
        //        stream.Position = 0;

        //        return File(stream.ToArray(), "application/pdf", $"Leave_{leave.Id}.pdf");
        //    }
        //}

        [Authorize]
        public async Task<IActionResult> ExportExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            bool isAdminOrManager = await _userManager.IsInRoleAsync(user, "Admin")
                                  || await _userManager.IsInRoleAsync(user, "Manager");

            var requests = await _leaveService.GetAllLeaveRequestsAsync();

            // Filter if not admin/manager
            var filtered = isAdminOrManager
                ? requests
                : requests.Where(l => l.RequestingUserId == user.Id);

            var stream = await _leaveService.ExportToExcelAsync(filtered);
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LeaveRequests.xlsx");
        }

        // Export PDF
        [Authorize]
        public async Task<IActionResult> ExportPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            bool isAdminOrManager = await _userManager.IsInRoleAsync(user, "Admin")
                                  || await _userManager.IsInRoleAsync(user, "Manager");

            var requests = await _leaveService.GetAllLeaveRequestsAsync();

            var filtered = isAdminOrManager
                ? requests
                : requests.Where(l => l.RequestingUserId == user.Id);

            var pdfBytes = await _leaveService.ExportToPdfAsync(filtered);
            return File(pdfBytes, "application/pdf", "LeaveRequests.pdf");
        }



        // Export all leave requests to Excel
        [Authorize]
        public async Task<IActionResult> ExportAllToExcel()
        {
            var leaves = await _leaveService.GetAllLeaveRequestsAsync();

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("LeaveRequests");

                // Header
                worksheet.Cells[1, 1].Value = "Id";
                worksheet.Cells[1, 2].Value = "First Name";
                worksheet.Cells[1, 3].Value = "Last Name";
                worksheet.Cells[1, 4].Value = "Start Date";
                worksheet.Cells[1, 5].Value = "End Date";
                worksheet.Cells[1, 6].Value = "Status";
                worksheet.Cells[1, 7].Value = "Reason";

                int row = 2;
                foreach (var leave in leaves)
                {
                    worksheet.Cells[row, 1].Value = leave.Id;
                    worksheet.Cells[row, 2].Value = leave.RequestingUser?.FirstName;
                    worksheet.Cells[row, 3].Value = leave.RequestingUser?.LastName;
                    worksheet.Cells[row, 4].Value = leave.StartDate.ToString("d");
                    worksheet.Cells[row, 5].Value = leave.EndDate.ToString("d");
                    worksheet.Cells[row, 6].Value = leave.Status;
                    worksheet.Cells[row, 7].Value = leave.Reason;
                    row++;
                }

                var bytes = package.GetAsByteArray();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AllLeaveRequests.xlsx");
            }
        }

        // Export all leave requests to PDF
        [Authorize]
        public async Task<IActionResult> ExportAllToPdf()
        {
            var leaves = await _leaveService.GetAllLeaveRequestsAsync();

            using (var stream = new MemoryStream())
            {
                var document = new PdfSharpCore.Pdf.PdfDocument();
                foreach (var leave in leaves)
                {
                    var page = document.AddPage();
                    var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                    var font = new PdfSharpCore.Drawing.XFont("Verdana", 12);

                    var text = $"Leave Request #{leave.Id}\n" +
                               $"Name: {leave.RequestingUser?.FirstName} {leave.RequestingUser?.LastName}\n" +
                               $"Dates: {leave.StartDate:d} to {leave.EndDate:d}\n" +
                               $"Status: {leave.Status}\n" +
                               $"Reason: {leave.Reason}";

                    gfx.DrawString(text, font, PdfSharpCore.Drawing.XBrushes.Black, new PdfSharpCore.Drawing.XRect(40, 60, page.Width - 80, page.Height - 60));
                }

                document.Save(stream, false);
                stream.Position = 0;

                return File(stream.ToArray(), "application/pdf", "AllLeaveRequests.pdf");
            }
        }

        [Authorize]
        public async Task<IActionResult> ExportFilteredToExcel(string search)
        {
            var user = await _userManager.GetUserAsync(User);
            bool isAdminOrManager = await _userManager.IsInRoleAsync(user, "Admin")
                                 || await _userManager.IsInRoleAsync(user, "Manager");

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

            var filtered = query.ToList();

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("FilteredLeaveRequests");

                // Header row
                worksheet.Cells["A1:F1"].LoadFromArrays(new object[][]
                {
            new object[] { "Id", "Employee Name", "Start Date", "End Date", "Status", "Reason" }
                });

                // Style header
                using (var range = worksheet.Cells["A1:F1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Fill data
                int row = 2;
                foreach (var leave in filtered)
                {
                    worksheet.Cells[row, 1].Value = leave.Id;
                    worksheet.Cells[row, 2].Value = $"{leave.RequestingUser?.FirstName} {leave.RequestingUser?.LastName}";
                    worksheet.Cells[row, 3].Value = leave.StartDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 4].Value = leave.EndDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 5].Value = leave.Status;
                    worksheet.Cells[row, 6].Value = leave.Reason;
                    row++;
                }

                // Auto-fit
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var bytes = package.GetAsByteArray();
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "FilteredLeaveRequests.xlsx");
            }
        }


        [Authorize]
        public async Task<IActionResult> ExportFilteredToPdf(string search)
        {
            var user = await _userManager.GetUserAsync(User);
            bool isAdminOrManager = await _userManager.IsInRoleAsync(user, "Admin")
                                  || await _userManager.IsInRoleAsync(user, "Manager");

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

            var filtered = query.ToList();

            using (var stream = new MemoryStream())
            {
                var document = new PdfSharpCore.Pdf.PdfDocument();

                var logoUrl = $"{Request.Scheme}://{Request.Host}/images/logo.png"; // your logo
                foreach (var leave in filtered)
                {
                    var page = document.AddPage();
                    var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);

                    var fontTitle = new PdfSharpCore.Drawing.XFont("Verdana", 14, PdfSharpCore.Drawing.XFontStyle.Bold);
                    var fontBody = new PdfSharpCore.Drawing.XFont("Verdana", 10);

                    // draw title
                    gfx.DrawString($"Leave Request #{leave.Id}", fontTitle, PdfSharpCore.Drawing.XBrushes.DarkBlue,
                        new PdfSharpCore.Drawing.XRect(40, 40, page.Width - 80, 20));

                    // draw data
                    var text = $"Name: {leave.RequestingUser?.FirstName} {leave.RequestingUser?.LastName}\n" +
                               $"Dates: {leave.StartDate:d} to {leave.EndDate:d}\n" +
                               $"Status: {leave.Status}\n" +
                               $"Reason: {leave.Reason}";

                    gfx.DrawString(text, fontBody, PdfSharpCore.Drawing.XBrushes.Black,
                        new PdfSharpCore.Drawing.XRect(40, 80, page.Width - 80, page.Height - 60));
                }

                document.Save(stream, false);
                stream.Position = 0;
                return File(stream.ToArray(), "application/pdf", "FilteredLeaveRequests.pdf");
            }
        }


    }
}
