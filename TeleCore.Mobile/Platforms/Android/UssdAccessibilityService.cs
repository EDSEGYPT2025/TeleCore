using Android.AccessibilityServices;
using Android.App;
using Android.OS;
using Android.Views.Accessibility;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Threading.Tasks;

namespace TeleCore.Mobile.Platforms.Android
{
    [Service(Label = "TeleCore Auto-Processor", Permission = global::Android.Manifest.Permission.BindAccessibilityService, Exported = true)]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    [MetaData("android.accessibilityservice", Resource = "@xml/ussd_service_config")]
    public class UssdAccessibilityService : AccessibilityService
    {
        public static string CurrentDecryptedPin = "";
        private bool _isTaskDone = false;
        private const string MyAppPackage = "com.telecore.secure.v3";

        // 🛑 إضافة متغيرات الذاكرة لمنع اللوب (Loop)
        private string _lastProcessedMessage = "";
        private DateTime _lastProcessedTime = DateTime.MinValue;

        public override void OnAccessibilityEvent(AccessibilityEvent e)
        {
            if (e == null) return;

            string packageName = e.PackageName?.ToString() ?? "";

            // 1. تجاهل تام لأي حدث داخل تطبيق TeleCore نفسه
            if (packageName == MyAppPackage) return;

            // 🛡️ 2. فلتر النطاق: منع قراءة شريط الإشعارات وتطبيقات الرسائل
            if (packageName.Contains("android.mms") ||
                packageName.Contains("google.android.apps.messaging") ||
                packageName.Contains("com.samsung.android.messaging") ||
                packageName.Contains("com.android.systemui"))
            {
                return;
            }

            // 3. الاستجابة فقط عند تغير الشباك (ظهور نافذة USSD)
            if (e.EventType != EventTypes.WindowStateChanged && e.EventType != EventTypes.WindowContentChanged) return;

            AccessibilityNodeInfo rootNode = RootInActiveWindow;
            if (rootNode == null) return;

            string screenContent = FlattenNodeText(rootNode);

            // 4. معالجة شاشات النهاية (إغلاق تلقائي وإرسال للسيرفر)
            if (screenContent.Contains("تم") || screenContent.Contains("نجاح") || screenContent.Contains("رصيد"))
            {
                // ✅ فلتر التكرار: لا ترسل نفس الرسالة إلا إذا مر 10 ثوانٍ على الأقل
                if (screenContent != _lastProcessedMessage || (DateTime.Now - _lastProcessedTime).TotalSeconds > 10)
                {
                    _lastProcessedMessage = screenContent;
                    _lastProcessedTime = DateTime.Now;

                    _isTaskDone = false;
                    CurrentDecryptedPin = ""; // تصفير للأمان

                    // إرسال الرسالة للسيرفر مرة واحدة فقط
                    WeakReferenceMessenger.Default.Send(screenContent);

                    // إغلاق النافذة
                    var okBtn = FindButton(rootNode, "موافق") ?? FindButton(rootNode, "OK") ?? FindButton(rootNode, "Cancel");
                    okBtn?.PerformAction(global::Android.Views.Accessibility.Action.Click);
                }
                return;
            }

            if (_isTaskDone || string.IsNullOrEmpty(CurrentDecryptedPin)) return;

            // 5. كشف حقل الـ PIN في نافذة النظام فقط
            AccessibilityNodeInfo inputField = FindInputField(rootNode);
            if (inputField != null && (screenContent.Contains("الرقم السري") || screenContent.Contains("PIN")))
            {
                _isTaskDone = true;

                // كتابة الـ PIN
                Bundle arguments = new Bundle();
                arguments.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, CurrentDecryptedPin);
                inputField.PerformAction(global::Android.Views.Accessibility.Action.SetText, arguments);

                // الضغط على إرسال بعد تأخير بسيط
                Task.Delay(500).ContinueWith(_ => {
                    var sendBtn = FindButton(rootNode, "إرسال") ?? FindButton(rootNode, "ارسال") ?? FindButton(rootNode, "Send");
                    sendBtn?.PerformAction(global::Android.Views.Accessibility.Action.Click);
                });
            }
        }

        private AccessibilityNodeInfo FindInputField(AccessibilityNodeInfo node)
        {
            if (node == null) return null;
            if (node.ClassName?.Contains("EditText") == true) return node;
            for (int i = 0; i < node.ChildCount; i++)
            {
                var res = FindInputField(node.GetChild(i));
                if (res != null) return res;
            }
            return null;
        }

        private AccessibilityNodeInfo FindButton(AccessibilityNodeInfo node, string text)
        {
            if (node == null) return null;
            if (node.Text?.ToString().Contains(text, StringComparison.OrdinalIgnoreCase) == true) return node;
            for (int i = 0; i < node.ChildCount; i++)
            {
                var res = FindButton(node.GetChild(i), text);
                if (res != null) return res;
            }
            return null;
        }

        private string FlattenNodeText(AccessibilityNodeInfo node)
        {
            if (node == null) return "";
            string text = node.Text?.ToString() ?? "";
            for (int i = 0; i < node.ChildCount; i++) text += " " + FlattenNodeText(node.GetChild(i));
            return text.Trim();
        }

        public override void OnInterrupt() { }
    }
}