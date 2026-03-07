using System.Threading.Tasks;

namespace TeleCore.Application.Services
{
    public interface ICommissionService
    {
        // دالة حساب العمولة والإجمالي (ديناميكية من الداتابيز)
        Task<(decimal Commission, decimal TotalAmount)> CalculateCommissionAsync(decimal amount, string transactionType);

        // دالة فحص حدود السحب والإيداع للشريحة
        Task<(bool IsAllowed, string ErrorMessage)> CheckSimLimitsAsync(int simCardId, decimal amount, string transactionType);
    }
}