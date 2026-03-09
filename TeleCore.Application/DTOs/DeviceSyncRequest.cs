using System.Collections.Generic;

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

        // 🟢 التعديل الجراحي: أصبحت خريطة (Dictionary) لتستوعب الـ ID مع رقم الهاتف
        // المفتاح (int) هو الـ SimId، والقيمة (string) هي رقم الهاتف
        public Dictionary<int, string> AssignedSims { get; set; } = new Dictionary<int, string>();
    }
}