namespace TeleCore.Domain.Entities
{
    public class CommissionRule
    {
        public int Id { get; set; }

        // ✅ إضافة نوع العملية (CashIn للإيداع، CashOut للسحب)
        public string TransactionType { get; set; } = string.Empty;

        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }

        public decimal FixedFee { get; set; }
        public decimal PercentageFee { get; set; }

        public decimal RoundingStep { get; set; }
        public bool IsActive { get; set; } = true;

    }
}