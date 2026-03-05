using SQLite;

namespace TeleCore.Mobile.Models
{
    public class TransactionRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Type { get; set; } // "تحويل" أو "استلام"
        public string ReceiverName { get; set; }
        public string ReceiverNumber { get; set; }
        public double Amount { get; set; }
        public string TransactionId { get; set; } // رقم العملية من رسالة SMS
        public double PostBalance { get; set; } // الرصيد بعد العملية
        public DateTime Timestamp { get; set; }
        public bool IsConfirmed { get; set; } // هل وصلت رسالة التأكيد؟

        public double ServiceFee { get; set; } // إضافة عمود مصاريف الخدمة

        // 🚀 الحقل الجديد للمزامنة الذكية
        public bool IsSynced { get; set; } = false;
    }
}