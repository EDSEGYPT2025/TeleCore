namespace TeleCore.Domain.Entities
{
    public class CommissionRule
    {
        public int Id { get; set; }
        public decimal MinAmount { get; set; } // الحد الأدنى للمبلغ (مثلاً 0)
        public decimal MaxAmount { get; set; } // الحد الأقصى (مثلاً 500)

        public decimal FixedFee { get; set; } // الرسوم الثابتة (مثلاً 5 جنيه)
        public decimal PercentageFee { get; set; } // النسبة (مثلاً 0.01 يعني 1%)

        // دي أهم حتة: التقريب لأقرب رقم (عشان تقرب الـ 12 لـ 15) -> قيمتها هتكون 5
        public decimal RoundingStep { get; set; }

        public bool IsActive { get; set; } = true;
    }
}