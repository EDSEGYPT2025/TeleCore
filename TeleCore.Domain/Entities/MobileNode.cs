namespace TeleCore.Domain.Entities
{
    public class MobileNode
    {
        public int Id { get; set; }

        // المعرف الفريد للجهاز (Hardware ID)
        public string DeviceUniqueId { get; set; } = string.Empty;

        // اسم الجهاز (Samsung J7, Infinix, etc.)
        public string DeviceModel { get; set; } = string.Empty;

        // المفتاح العام للتشفير (RSA Public Key)
        public string PublicKey { get; set; } = string.Empty;

        // حالة الجهاز
        public bool IsAuthorized { get; set; } = false; // لا يعمل إلا بموافقة الإدارة
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        // الربط بالفرع (حسب ملف Branch.cs عندك)
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        // ربط الجهاز بالشرائح الحالية (حسب ملف SimCard.cs عندك)
        // الموبايل الواحد ممكن يشيل أكتر من شريحة (Sim 1, Sim 2)
        public ICollection<SimCard> AssignedSimCards { get; set; } = new List<SimCard>();
    }
}