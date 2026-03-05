using TeleCore.Domain.Entities;
using TeleCore.Application.Common;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace TeleCore.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICommissionService _commissionService;

        public TransactionService(IApplicationDbContext context, ICommissionService commissionService)
        {
            _context = context;
            _commissionService = commissionService;
        }

        public async Task<(Transaction transaction, string encryptedPin)> ExecuteTransferAsync(int simId, string targetNumber, decimal amount, string clearPin)
        {
            // 1. حساب العمولة
            var (commission, total) = _commissionService.CalculateCommission(amount);

            // 2. جلب بيانات الشريحة
            var sim = await _context.SimCards.FirstOrDefaultAsync(s => s.Id == simId);
            if (sim == null || sim.CurrentBalance < amount)
                throw new Exception("رصيد الشريحة لا يكفي أو الشريحة غير موجودة");

            // 3. 🛡️ البحث عن الموبايل المعتمد لفرع هذه الشريحة
            var mobileNode = await _context.MobileNodes
                .FirstOrDefaultAsync(n => n.BranchId == sim.BranchId && n.IsAuthorized);

            if (mobileNode == null)
                throw new Exception("لا يوجد جهاز موبايل معتمد أو متصل لفرع هذه الشريحة لتنفيذ العملية.");

            // 4. 🔒 عملية التشفير (Encryption) باستخدام مفتاح الموبايل
            using var rsa = new RSACryptoServiceProvider(2048);
            rsa.FromXmlString(mobileNode.PublicKey);
            var pinBytes = Encoding.UTF8.GetBytes(clearPin);
            var encryptedBytes = rsa.Encrypt(pinBytes, false);
            string encryptedPinBase64 = Convert.ToBase64String(encryptedBytes);

            // 5. تحديث الأرصدة (خصم من الشريحة)
            sim.CurrentBalance -= amount;

            // 6. إنشاء سجل العملية
            var transaction = new Transaction
            {
                SimCardId = simId,
                TargetNumber = targetNumber,
                Amount = amount,
                Commission = commission,
                TotalAmount = total,
                Type = "CashOut",
                Status = "Pending", // ⏳ خليناها Pending لأن الموبايل لسه هينفذ الـ USSD
                CreatedAt = DateTime.UtcNow,
                TransactionReference = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()
            };

            _context.Transactions.Add(transaction);

            // 7. حفظ كل التغييرات في خبطة واحدة للداتابيز
            await _context.SaveChangesAsync();

            // إرجاع العملية والـ PIN المشفر عشان الكاشير يبعتهم للموبايل
            return (transaction, encryptedPinBase64);
        }
    }
}