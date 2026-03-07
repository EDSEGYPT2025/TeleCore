using System.Collections.Generic;

namespace TeleCore.Application.DTOs
{
    public class OpenShiftRequest
    {
        public int CashierId { get; set; }
        public int DrawerId { get; set; }
        public decimal OpeningCashBalance { get; set; }

        // قائمة بأرقام (Id) الخطوط التي اختار الكاشير العمل عليها اليوم
        public List<int> SelectedSimCardIds { get; set; } = new List<int>();
    }
}