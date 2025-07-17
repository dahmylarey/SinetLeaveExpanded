using System.Threading.Tasks;

namespace SinetLeaveManagement.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerateLeaveRequestPdfAsync(int leaveRequestId);
    }
}
