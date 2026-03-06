namespace TeleCore.Domain.Entities
{
    public class ShiftSimCard
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public int SimCardId { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }

        // ✅ إضافة العلاقات لسهولة استخراج التقارير
        public Shift Shift { get; set; }
        public SimCard SimCard { get; set; }
    }
}