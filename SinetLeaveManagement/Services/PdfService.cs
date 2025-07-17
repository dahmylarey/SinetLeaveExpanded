using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using SinetLeaveManagement.Data;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
    public class PdfService : IPdfService
    {
        private readonly ApplicationDbContext _context;

        public PdfService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateLeaveRequestPdfAsync(int id, ControllerContext controllerContext)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                var pdfBytes = await new ViewAsPdf("PdfTemplate", leave)
                {
                    PageSize = Rotativa.AspNetCore.Options.Size.A4
                }.BuildFile(controllerContext);

                return pdfBytes;
            }

            return null; // or throw new Exception("Leave not found");
        }


        // This method is not implemented yet, but you can use the above method as a reference.
        public Task<byte[]> GenerateLeaveRequestPdfAsync(int id)
        {

            throw new NotImplementedException();
        }
    }
}
