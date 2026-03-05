using System;
using System.Collections.Generic;

namespace TeleCore.Domain.Entities
{
    public class Shift
    {
        public int Id { get; set; }
        public int CashierId { get; set; }
        public int DrawerId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsClosed { get; set; }

        // عهدة الدرج (الكاش)
        public decimal OpeningDrawerBalance { get; set; }
        public decimal? ClosingDrawerBalance { get; set; } // الفعلي وقت التقفيل
        public decimal? ShortageOrSurplus { get; set; } // العجز أو الزيادة

        // علاقة: الوردية فيها كذا شريحة (موبايل)
        public ICollection<ShiftSimCard> ShiftSimCards { get; set; } = new List<ShiftSimCard>();
    }
}