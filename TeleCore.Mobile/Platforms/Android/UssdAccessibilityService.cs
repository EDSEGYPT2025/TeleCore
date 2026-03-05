using Android.AccessibilityServices;
using Android.App;
using Android.OS;
using Android.Views.Accessibility;
using CommunityToolkit.Mvvm.Messaging;

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

        public override void OnAccessibilityEvent(AccessibilityEvent e)
        {
            if (e == null) return;

            // 🛑 1. تجاهل تام لأي حدث داخل تطبيق TeleCore نفسه
            if (e.PackageName?.ToString() == MyAppPackage) return;

            // ✅ 2. الاستجابة فقط عند تغير الشباك (ظهور نافذة USSD)
            if (e.EventType != EventTypes.WindowStateChanged && e.EventType != EventTypes.WindowContentChanged) return;

            AccessibilityNodeInfo rootNode = RootInActiveWindow;
            if (rootNode == null) return;

            string screenContent = FlattenNodeText(rootNode);

            // 3. معالجة شاشات النهاية (إغلاق تلقائي)
            if (screenContent.Contains("تم") || screenContent.Contains("نجاح") || screenContent.Contains("رصيد"))
            {
                _isTaskDone = false;
                WeakReferenceMessenger.Default.Send(screenContent);
                CurrentDecryptedPin = ""; // تصفير للأمان

                var okBtn = FindButton(rootNode, "موافق") ?? FindButton(rootNode, "OK");
                okBtn?.PerformAction(global::Android.Views.Accessibility.Action.Click);
                return;
            }

            if (_isTaskDone || string.IsNullOrEmpty(CurrentDecryptedPin)) return;

            // 4. كشف حقل الـ PIN في نافذة النظام فقط
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
            return text;
        }

        public override void OnInterrupt() { }
    }
}