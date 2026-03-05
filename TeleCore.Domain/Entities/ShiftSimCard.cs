namespace TeleCore.Domain.Entities
{
    public class ShiftSimCard
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public int SimCardId { get; set; }

        public decimal OpeningBalance { get; set; } // رصيد الشريحة أول ما استلمها
        public decimal? ClosingBalance { get; set; } // رصيدها لما قفل الوردية
    }
}