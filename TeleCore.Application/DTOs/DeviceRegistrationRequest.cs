namespace TeleCore.Application.DTOs
{
    public class DeviceRegistrationRequest
    {
        public string DeviceUniqueId { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty; // اختياري لاحقاً للتشفير
    }
}