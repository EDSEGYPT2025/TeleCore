using TeleCore.Domain.Entities;
using TeleCore.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace TeleCore.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IApplicationDbContext _context; // نستخدم الـ Interface هنا
        private readonly ICommissionService _commissionService;

        public TransactionService(IApplicationDbContext context, ICommissionService commissionService)
        {
            _context = context;
            _commissionService = commissionService;
        }

        public async Task<Transaction> ExecuteTransferAsync(int simId, string targetNumber, decimal amount)
        {
            // 1. حساب العمولة
            var (commission, total) = _commissionService.CalculateCommission(amount);

            // 2. جلب بيانات الشريحة
            var sim = await _context.SimCards.FirstOrDefaultAsync(s => s.Id == simId);
            if (sim == null || sim.CurrentBalance < amount)
                throw new Exception("رصيد الشريحة لا يكفي أو الشريحة غير موجودة");

            // 3. تحديث الأرصدة (خصم من الشريحة)
            sim.CurrentBalance -= amount;

            // 4. إنشاء سجل العملية
            var transaction = new Transaction
            {
                SimCardId = simId,
                TargetNumber = targetNumber,
                Amount = amount,
                Commission = commission,
                TotalAmount = total,
                Type = "CashOut",
                Status = "Completed", // في البداية نفترض النجاح، ولاحقاً نربطها برد الموبايل
                CreatedAt = DateTime.UtcNow,
                TransactionReference = Guid.NewGuid().ToString() // المايجريشن طالب الحقل ده ضروري
            };

            _context.Transactions.Add(transaction);

            // 5. حفظ كل التغييرات في خبطة واحدة للداتابيز
            await _context.SaveChangesAsync();

            return transaction;
        }
    }
}