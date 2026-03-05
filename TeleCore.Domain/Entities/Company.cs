namespace TeleCore.Domain.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true; // لإيقاف الشركة لو مدفعتش الاشتراك
        public DateTime SubscriptionExpiryDate { get; set; }

        // Navigation Property: الشركة ليها فروع كتير
        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }
}