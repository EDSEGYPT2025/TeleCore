namespace TeleCore.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionReference { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

        public int SimCardId { get; set; }
        public SimCard SimCard { get; set; }

        public string TargetNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public decimal Commission { get; set; }

        // ✅ جديد: رسوم الشبكة (تخصم من المحفظة الفعلية وتستخرج من رسالة الموبايل)
        public decimal NetworkFee { get; set; } = 0;

        public decimal TotalAmount { get; set; }
        public string Type { get; set; } // CashIn, CashOut
        public string Status { get; set; } = "Pending";

        // ✅ جديد: رسالة فودافون الأصلية للمراجعة
        public string? ProviderResponseMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ShiftId { get; set; }
        public int? CashierId { get; set; }
    }
}