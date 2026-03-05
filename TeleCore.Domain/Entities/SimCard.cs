namespace TeleCore.Domain.Entities
{
    public class SimCard
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty; // رقم الشريحة
        public string Provider { get; set; } = string.Empty; // فودافون، اتصالات، الخ

        public decimal CurrentBalance { get; set; } = 0; // الرصيد الحالي على الشريحة

        public string? AssignedDeviceId { get; set; }

        // حدود الاستخدام
        public decimal DailyLimit { get; set; } = 60000;
        public decimal MonthlyLimit { get; set; } = 200000;

        public bool IsActive { get; set; } = true;

        public Branch Branch { get; set; }
    }
}