namespace TeleCore.Domain.Entities
{
    public class Branch
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;

        // ✅ جديد: عنوان الفرع وحالته
        public string Location { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public decimal DrawerCashBalance { get; set; } = 0;

        public Company Company { get; set; }
        public ICollection<SimCard> SimCards { get; set; } = new List<SimCard>();
    }
}