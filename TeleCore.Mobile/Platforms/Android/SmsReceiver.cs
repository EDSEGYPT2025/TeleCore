using Android.App;
using Android.Content;
using System.Text;
using System.Text.RegularExpressions;
using TeleCore.Mobile.Models;
using TeleCore.Mobile.Services;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Threading.Tasks;

namespace TeleCore.Mobile.Platforms.Android
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "android.provider.Telephony.SMS_RECEIVED" }, Priority = (int)IntentFilterPriority.HighPriority)]
    public class SmsReceiver : BroadcastReceiver
    {
        private static string _lastProcessedBody = string.Empty;
        private readonly DatabaseService _db = new DatabaseService();

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != "android.provider.Telephony.SMS_RECEIVED") return;

            try
            {
                var messages = global::Android.Provider.Telephony.Sms.Intents.GetMessagesFromIntent(intent);
                if (messages == null || messages.Length == 0) return;

                string sender = messages[0].DisplayOriginatingAddress ?? "";

                // 1. فلترة دقيقة للمرسل
                if (IsCashProvider(sender))
                {
                    string body = ExtractMessageBody(messages);

                    // منع التكرار الناتج عن تقسيم الرسائل الطويلة
                    if (_lastProcessedBody == body) return;
                    _lastProcessedBody = body;

                    // معالجة الرسالة في الخلفية لضمان عدم ثقل النظام
                    _ = ProcessSmsAsync(body, sender);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmsReceiver] Critical Error: {ex.Message}");
            }
        }

        private bool IsCashProvider(string sender) =>
            sender.Contains("VF-Cash", StringComparison.OrdinalIgnoreCase) ||
            sender.Contains("VFCash", StringComparison.OrdinalIgnoreCase) ||
            sender.Equals("6001");

        private string ExtractMessageBody(global::Android.Telephony.SmsMessage[] messages)
        {
            var sb = new StringBuilder();
            foreach (var msg in messages) sb.Append(msg.DisplayMessageBody);
            return sb.ToString();
        }

        private async Task ProcessSmsAsync(string body, string sender)
        {
            try
            {
                bool isIncoming = body.Contains("استلام") || body.Contains("إيداع") || body.Contains("ايداع");
                bool isOutgoing = body.Contains("تحويل") && (body.Contains("تم") || body.Contains("خصم"));

                if (isIncoming)
                {
                    await HandleIncomingTransaction(body, sender);
                }
                else if (isOutgoing)
                {
                    await HandleOutgoingTransaction(body);
                }

                // تحديث الواجهة بشكل لحظي
                NotifyUiRefresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmsReceiver] Processing Error: {ex.Message}");
            }
        }

        private async Task HandleIncomingTransaction(string body, string sender)
        {
            double amount = ExtractDouble(body, @"مبلغ\s*([\d\.]+)");
            double balance = ExtractDouble(body, @"الحالي.*?([\d\.]+)");
            string txId = ExtractGroup(body, @"رقم العملي[ةه].*?(\d+)", "غير متوفر");
            string actualSenderNumber = ExtractGroup(body, @"رقم\s*(01\d{9})", sender);

            string senderName = ExtractGroup(body, @"ب[إا]سم\s+(.+?)(?=\s+رصيدك)", "");
            if (string.IsNullOrEmpty(senderName))
            {
                senderName = ExtractGroup(body, @"\((.+?)\)", "");
            }
            senderName = string.IsNullOrWhiteSpace(senderName) ? "إيداع نقدي" : senderName.Trim();

            var record = new TransactionRecord
            {
                Type = "استقبال",
                ReceiverName = senderName,
                Amount = amount,
                Timestamp = DateTime.Now,
                IsConfirmed = true,
                TransactionId = txId,
                PostBalance = balance,
                ReceiverNumber = actualSenderNumber,
                ServiceFee = 0 // مفيش مصاريف في الاستقبال غالباً
            };

            await _db.SaveTransaction(record);

            // 🚀 إرسال العملية للسيرفر
            await SyncWithServerAsync(record);
        }

        private async Task HandleOutgoingTransaction(string body)
        {
            double amount = ExtractDouble(body, @"مبلغ\s*([\d\.]+)");
            if (amount == 0) amount = ExtractDouble(body, @"(?:تحويل|خصم).*?([\d\.]+)");
            if (amount <= 0) return;

            double balance = ExtractDouble(body, @"رصيد[^\d]*([\d\.]+)");
            double fee = ExtractDouble(body, @"مصاريف[^\d]*([\d\.]+)");
            if (fee == 0) fee = ExtractDouble(body, @"رسوم[^\d]*([\d\.]+)");

            string txId = ExtractGroup(body, @"عملي[ةه][^\d]*(\d+)", "غير متوفر");
            string receiverNumber = ExtractGroup(body, @"رقم\s*(01\d{9})", "غير معروف");

            var record = new TransactionRecord
            {
                Type = "تحويل",
                ReceiverName = "تحويل صادر",
                Amount = amount,
                Timestamp = DateTime.Now,
                IsConfirmed = true,
                TransactionId = txId,
                PostBalance = balance,
                ReceiverNumber = receiverNumber,
                ServiceFee = fee
            };

            await _db.SaveTransaction(record);

            // 🚀 إرسال العملية للسيرفر
            await SyncWithServerAsync(record);
        }

        // ==========================================
        // 🚀 دالة المزامنة الموحدة مع السيرفر
        // ==========================================
        private async Task SyncWithServerAsync(TransactionRecord record)
        {
            try
            {
                var apiService = new ApiService();

                // بنبعت الريكورد كامل للسيرفر
                bool isSynced = await apiService.SyncSingleTransactionAsync(record);

                if (isSynced)
                {
                    // لو السيرفر استلمها بنجاح، نعلم عليها في الداتا بيز المحلية إنها اترفعت
                    await _db.MarkAsSyncedAsync(record.Id);
                    System.Diagnostics.Debug.WriteLine($"[TeleCore] 🌐 تم تسميع العملية {record.TransactionId} في السيرفر بنجاح!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[TeleCore] ⚠️ السيرفر لم يستجب، العملية {record.TransactionId} مسجلة محلياً فقط (سيتم رفعها لاحقاً).");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] ❌ خطأ في الاتصال بالسيرفر أثناء المزامنة: {ex.Message}");
            }
        }

        #region Helpers (Regex Engine)

        private double ExtractDouble(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            return match.Success && double.TryParse(match.Groups[1].Value, out double result) ? result : 0;
        }

        private string ExtractGroup(string input, string pattern, string defaultValue)
        {
            var match = Regex.Match(input, pattern);
            return match.Success ? match.Groups[1].Value : defaultValue;
        }

        private void NotifyUiRefresh()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send("RefreshHistory");
            });
        }

        #endregion
    }
}