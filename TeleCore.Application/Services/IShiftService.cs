using System.Threading.Tasks;
using TeleCore.Application.DTOs;

namespace TeleCore.Application.Services
{
    public interface IShiftService
    {
        // دالة لفتح الوردية
        Task<(bool Success, string Message, int? ShiftId)> OpenShiftAsync(OpenShiftRequest request);
    }
}