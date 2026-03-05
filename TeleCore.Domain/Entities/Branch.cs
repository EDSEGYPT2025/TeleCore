using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TeleCore.Domain.Entities
{
    public class Branch
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;

        // رصيد الدرج (الكاشير) في هذا الفرع
        public decimal DrawerCashBalance { get; set; } = 0;

        public Company Company { get; set; } // العلاقة مع الشركة
        public ICollection<SimCard> SimCards { get; set; } = new List<SimCard>();
    }
}