namespace TeleCore.Domain.Entities
{
    public class Cashier
    {
        public int Id { get; set; }
        public int BranchId { get; set; } // مربوط بفرع معين
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}