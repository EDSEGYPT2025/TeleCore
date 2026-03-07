using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TeleCore.Application.Common; // تأكد من مسار الـ DbContext

namespace TeleCore.Application.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly IApplicationDbContext _context;

        public CommissionService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(decimal Commission, decimal TotalAmount)> CalculateCommissionAsync(decimal amount, string transactionType)
        {
            decimal commission = 0;

            // 1. جلب القاعدة المناسبة من الداتابيز بناءً على المبلغ ونوع العملية
            var rule = await _context.CommissionRules
                .Where(r => r.IsActive &&
                            r.TransactionType == transactionType &&
                            amount >= r.MinAmount &&
                            amount <= r.MaxAmount)
                .FirstOrDefaultAsync();

            if (rule != null)
            {
                // 2. حساب العمولة (ثابتة + نسبة)
                commission = rule.FixedFee + (amount * rule.PercentageFee);

                // 3. معادلتك الذكية للتقريب، لكن باستخدام الرقم القادم من الداتابيز
                if (rule.RoundingStep > 0 && commission > 0)
                {
                    commission = Math.Ceiling(commission / rule.RoundingStep) * rule.RoundingStep;
                }
            }

            decimal totalAmount = amount + commission;

            return (commission, totalAmount);
        }

        public async Task<(bool IsAllowed, string ErrorMessage)> CheckSimLimitsAsync(int simCardId, decimal amount, string transactionType)
        {
            var sim = await _context.SimCards.FindAsync(simCardId);
            if (sim == null) return (false, "الشريحة غير موجودة على النظام.");

            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // جلب عمليات الشريحة خلال الشهر الحالي
            var transactions = await _context.Transactions
                .Where(t => t.SimCardId == simCardId &&
                            t.Type == transactionType &&
                           (t.Status == "Completed" || t.Status == "Pending") &&
                            t.CreatedAt >= startOfMonth)
                .ToListAsync();

            decimal dailyTotal = transactions.Where(t => t.CreatedAt >= today).Sum(t => t.Amount);
            decimal monthlyTotal = transactions.Sum(t => t.Amount);

            // التحقق من الليمت بناءً على نوع العملية
            if (transactionType == "CashOut") // سحب/إرسال
            {
                if (dailyTotal + amount > sim.DailyWithdrawLimit)
                    return (false, $"تم تجاوز الحد اليومي للسحب. المتبقي: {sim.DailyWithdrawLimit - dailyTotal} ج.م");

                if (monthlyTotal + amount > sim.MonthlyWithdrawLimit)
                    return (false, $"تم تجاوز الحد الشهري للسحب. المتبقي: {sim.MonthlyWithdrawLimit - monthlyTotal} ج.م");
            }
            else if (transactionType == "CashIn") // إيداع/استقبال
            {
                if (dailyTotal + amount > sim.DailyDepositLimit)
                    return (false, $"تم تجاوز الحد اليومي للإيداع. المتبقي: {sim.DailyDepositLimit - dailyTotal} ج.م");

                if (monthlyTotal + amount > sim.MonthlyDepositLimit)
                    return (false, $"تم تجاوز الحد الشهري للإيداع. المتبقي: {sim.MonthlyDepositLimit - monthlyTotal} ج.م");
            }

            return (true, string.Empty);
        }
    }
}