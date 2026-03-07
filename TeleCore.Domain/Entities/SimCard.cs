namespace TeleCore.Domain.Entities
{
    public class SimCard
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;

        public decimal CurrentBalance { get; set; } = 0;
        public string? AssignedDeviceId { get; set; }

        // 🟢 الإضافة السحرية التي ستحل جميع الأخطاء (ربط الشريحة بالموبايل)
        public int? MobileNodeId { get; set; }
        public MobileNode? MobileNode { get; set; }

        // ✅ البيانات الإضافية الجديدة للمحفظة
        public string OwnerName { get; set; } = string.Empty; // اسم صاحب المحفظة
        public string? NationalId { get; set; } // الرقم القومي (اختياري)
        public string? SerialNumber { get; set; } // سيريال الشريحة (اختياري)

        // ✅ تفصيل حدود الاستخدام (سحب وإيداع) بدلاً من الليمت العام
        public decimal DailyWithdrawLimit { get; set; } = 60000;
        public decimal MonthlyWithdrawLimit { get; set; } = 200000;
        public decimal DailyDepositLimit { get; set; } = 60000;
        public decimal MonthlyDepositLimit { get; set; } = 200000;

        // احتفظنا بالقديم مؤقتاً عشان لو مستخدم في حتة في الكود ميتضربش (ممكن نمسحه لاحقاً)
        public decimal DailyLimit { get; set; } = 60000;
        public decimal MonthlyLimit { get; set; } = 200000;

        public bool IsActive { get; set; } = true;
        public Branch Branch { get; set; }
    }
}