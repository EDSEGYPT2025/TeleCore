using Android.Content;
using Android.Telecom;
using Application = Android.App.Application;
using TeleCore.Mobile.Services;
using System;

namespace TeleCore.Mobile.Platforms.Android
{
    public class AndroidUssdService : IUssdService
    {
        public void DialUssd(string code, int simId)
        {
            try
            {
                string encodedHash = global::Android.Net.Uri.Encode("#");
                string ussdCode = code.Replace("#", encodedHash);
                var uri = global::Android.Net.Uri.Parse($"tel:{ussdCode}");

                var intent = new Intent(Intent.ActionCall, uri);
                intent.AddFlags(ActivityFlags.NewTask);

                // 🚀 الجزء السحري: اختيار الشريحة الصحيحة للاتصال
                var telecomManager = (TelecomManager)Application.Context.GetSystemService(Context.TelecomService);

                if (telecomManager != null && telecomManager.CallCapablePhoneAccounts != null)
                {
                    var phoneAccounts = telecomManager.CallCapablePhoneAccounts;

                    // تحويل simId لـ Index (لو السيرفر بيبعت 1 يعني SIM 1، إذن الـ Index هو 0)
                    int slotIndex = simId - 1;

                    if (phoneAccounts.Count > slotIndex && slotIndex >= 0)
                    {
                        // نحدد الشريحة المطلوبة للاتصال في الـ Intent
                        intent.PutExtra(TelecomManager.ExtraPhoneAccountHandle, phoneAccounts[slotIndex]);
                        System.Diagnostics.Debug.WriteLine($"[TeleCore] 📲 جاري الاتصال باستخدام شريحة رقم: {simId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[TeleCore] ⚠️ الشريحة المطلوبة غير موجودة في الهاتف، سيتم استخدام الشريحة الافتراضية.");
                    }
                }

                Application.Context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error dialing USSD: {ex.Message}");
            }
        }
    }
}