namespace TeleCore.Application.DTOs
{
    // 📤 ما يرسله الموبايل للسيرفر
    public class DeviceSyncRequest
    {
        public string DeviceUniqueId { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
    }

    // 📥 ما يرد به السيرفر على الموبايل
    public class DeviceSyncResponse
    {
        public bool Success { get; set; }
        public bool IsAuthorized { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AssignedSimNumber { get; set; } // رقم الشريحة المربوطة بالجهاز
        public string Provider { get; set; } // شبكة الشريحة (فودافون/أورانج)
    }
}