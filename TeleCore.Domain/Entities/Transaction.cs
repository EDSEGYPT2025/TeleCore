namespace TeleCore.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionReference { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

        public int SimCardId { get; set; } // الشريحة اللي نفذت
        public SimCard SimCard { get; set; }

        public string TargetNumber { get; set; } = string.Empty; // رقم العميل المحول له أو منه

        public decimal Amount { get; set; } // المبلغ الأصلي (مثلاً 1200)
        public decimal Commission { get; set; } // عمولة النظام (مثلاً 15 بعد التقريب)
        public decimal TotalAmount { get; set; } // الإجمالي (1215)

        public string Type { get; set; } // "CashIn" إيداع, "CashOut" تحويل
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ShiftId { get; set; } // رقم الوردية اللي اتعملت فيها العملية
        public int? CashierId { get; set; } // الكاشير اللي نفذها
    }
}