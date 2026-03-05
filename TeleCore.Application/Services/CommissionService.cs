using System;

namespace TeleCore.Application.Services
{
    public class CommissionService : ICommissionService
    {
        public (decimal Commission, decimal TotalAmount) CalculateCommission(decimal amount)
        {
            decimal commission = 0;

            if (amount < 500)
            {
                commission = 5;
            }
            else if (amount >= 500 && amount <= 1000)
            {
                commission = 10;
            }
            else if (amount > 1000)
            {
                // حساب 1% من المبلغ
                commission = amount * 0.01m;

                // التقريب لأقرب 5 جنيه للأعلى (مثال: 12 تقرب لـ 15)
                commission = Math.Ceiling(commission / 5.0m) * 5.0m;
            }

            decimal totalAmount = amount + commission;

            return (commission, totalAmount);
        }
    }
}