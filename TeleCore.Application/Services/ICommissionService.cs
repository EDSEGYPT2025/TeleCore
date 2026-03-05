namespace TeleCore.Application.Services
{
    public interface ICommissionService
    {
        // دالة بتاخد المبلغ الأصلي، وبترجع (العمولة، والإجمالي)
        (decimal Commission, decimal TotalAmount) CalculateCommission(decimal amount);
    }
}