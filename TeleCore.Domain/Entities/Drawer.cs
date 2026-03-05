namespace TeleCore.Domain.Entities
{
    public class Drawer
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string Name { get; set; } = string.Empty; // مثلاً: درج كاشير 1
        public decimal CurrentBalance { get; set; } // الفلوس اللي فيه حالياً
        public bool IsActive { get; set; } = true;
    }
}