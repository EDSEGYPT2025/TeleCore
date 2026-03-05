using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Views.Accessibility;
using Android.OS;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeleCore.Mobile.Models;
using TeleCore.Mobile.Services;

namespace TeleCore.Mobile.Platforms.Android
{
    [Service(Label = "TeleCore Auto-PIN", Permission = global::Android.Manifest.Permission.BindAccessibilityService, Exported = true)]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    [MetaData("android.accessibilityservice", Resource = "@xml/ussd_service_config")]
    public class UssdAccessibilityService : AccessibilityService
    {
        private string _testPinCode = "156198"; // الرمز السري
        private bool _isTaskDone = false;

        private string _lastAmount = "";
        private string _lastReceiverNumber = "";
        private string _lastReceiverName = "";

        public override void OnAccessibilityEvent(AccessibilityEvent e)
        {
            if (e == null) return;

            string packageName = e.PackageName?.ToString() ?? "";

            // 1. إذا خرجنا من تطبيق الاتصال، قم بتصفير القفل استعداداً للعملية القادمة
            if (!packageName.Contains("com.android.phone") &&
                !packageName.Contains("com.android.server.telecom") &&
                !packageName.Contains("com.sec.android.app.launcher"))
            {
                _isTaskDone = false;
                return;
            }

            AccessibilityNodeInfo rootNode = RootInActiveWindow;
            if (rootNode == null)
            {
                _isTaskDone = false;
                return;
            }

            string screenContent = FlattenNodeText(rootNode);

            // 2. المنع الذكي: شاشات النهاية (النجاح أو الفشل)
            if (screenContent.Contains("رصيد") || screenContent.Contains("balance") ||
                screenContent.Contains("تم") || screenContent.Contains("نجاح"))
            {
                _isTaskDone = false; // فك القفل

                // إضافة احترافية: الضغط على "موافق" لغلق شاشة النجاح تلقائياً
                AccessibilityNodeInfo okBtn = FindButton(rootNode, "موافق") ?? FindButton(rootNode, "OK");
                okBtn?.PerformAction(global::Android.Views.Accessibility.Action.Click);
                return;
            }

            // إذا كانت المهمة قيد التنفيذ، تجاهل أي تحديثات في الشاشة
            if (_isTaskDone) return;

            AccessibilityNodeInfo inputField = FindInputField(rootNode);

            // 3. تأكيد أن هذه هي "شاشة التحويل الأساسية" وليست شاشة تأكيد فرعية
            bool isMainTransferScreen = screenContent.Contains("تحويل") || screenContent.Contains("بإسم");

            // --- التعديل السحري هنا ---
            // إزالة شرط inputField.ViewIdResourceName == null للسماح بالكتابة في الشاشات الحديثة
            if (inputField != null && isMainTransferScreen && !_isTaskDone)
            {
                _isTaskDone = true; // إغلاق القفل فوراً لمنع التكرار

                ExtractTransactionData(screenContent);

                // --- جدار الحماية (Hard Guard) ---
                if (string.IsNullOrEmpty(_lastAmount) || string.IsNullOrEmpty(_lastReceiverNumber))
                {
                    _isTaskDone = false; // إنذار كاذب، فك القفل
                    return;
                }

                // انتظار بسيط جداً لضمان استقرار الشاشة قبل الكتابة
                System.Threading.Thread.Sleep(300);

                // كتابة الرقم السري مباشرة
                Bundle arguments = new Bundle();
                arguments.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, _testPinCode);
                inputField.PerformAction(global::Android.Views.Accessibility.Action.SetText, arguments);

                // حفظ العملية السليمة في الداتابيز
                SaveToDb();

                // انتظار نصف ثانية قبل الضغط لضمان ظهور الزر واستيعاب النظام للنص
                System.Threading.Thread.Sleep(500);

                AccessibilityNodeInfo sendButton = FindButton(rootNode, "Send") ??
                                                   FindButton(rootNode, "إرسال") ??
                                                   FindButton(rootNode, "ارسال") ??
                                                   FindButton(rootNode, "موافق") ??
                                                   FindButton(rootNode, "OK");

                if (sendButton != null)
                {
                    sendButton.PerformAction(global::Android.Views.Accessibility.Action.Click);
                }
                else
                {
                    PerformClickOnAnyButton(rootNode);
                }
            }
        }

        private void SaveToDb()
        {
            try
            {
                double.TryParse(_lastAmount, out double amount);

                // --- جدار الحماية للداتابيز ---
                if (amount <= 0 || string.IsNullOrEmpty(_lastReceiverNumber))
                {
                    return;
                }

                var db = new DatabaseService();
                var record = new TransactionRecord
                {
                    Type = "تحويل",
                    ReceiverName = string.IsNullOrEmpty(_lastReceiverName) ? "مستلم غير معروف" : _lastReceiverName,
                    ReceiverNumber = _lastReceiverNumber,
                    Amount = amount,
                    Timestamp = System.DateTime.Now,
                    IsConfirmed = false
                };

                Task.Run(async () => {
                    await db.SaveTransaction(record);
                    System.Diagnostics.Debug.WriteLine($"[TeleCore] DB Saved: {record.ReceiverName} - {amount} EGP");
                });

                // تصفير البيانات بعد الإرسال للحفظ
                _lastAmount = "";
                _lastReceiverNumber = "";
                _lastReceiverName = "";
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] DB Error: {ex.Message}");
            }
        }

        private void ExtractTransactionData(string text)
        {
            try
            {
                // 1. استخراج المبلغ (يقرأ الرقم اللي بعد كلمة تحويل مباشرة)
                var amountMatch = Regex.Match(text, @"تحويل\s*(\d+(?:\.\d+)?)");
                if (amountMatch.Success) _lastAmount = amountMatch.Groups[1].Value;

                // 2. استخراج رقم المستلم (11 رقم يبدأ بـ 01)
                var numberMatch = Regex.Match(text, @"\b01[0125]\d{8}\b");
                if (numberMatch.Success) _lastReceiverNumber = numberMatch.Value;

                // 3. استخراج الاسم بذكاء (يقرأ اللي بعد "بإسم" ويقف عند أول رقم يقابله زي 0:مزيد)
                var nameMatch = Regex.Match(text, @"بإسم\s+(.*?)(?=\s*\d|$)");
                if (nameMatch.Success)
                {
                    // تنظيف الاسم من النجوم والمسافات الزائدة
                    _lastReceiverName = nameMatch.Groups[1].Value.Replace("*", "").Trim();
                }

                System.Diagnostics.Debug.WriteLine($"[TeleCore] USSD Extracted: Name={_lastReceiverName}, Amount={_lastAmount}, Number={_lastReceiverNumber}");
            }
            catch { }
        }

        private void PerformClickOnAnyButton(AccessibilityNodeInfo node)
        {
            if (node == null) return;
            if (node.Clickable && (node.ClassName.Contains("Button") || node.ClassName.Contains("TextView")))
            {
                node.PerformAction(global::Android.Views.Accessibility.Action.Click);
                return;
            }
            for (int i = 0; i < node.ChildCount; i++)
            {
                PerformClickOnAnyButton(node.GetChild(i));
            }
        }

        private AccessibilityNodeInfo FindInputField(AccessibilityNodeInfo node)
        {
            if (node == null) return null;
            if (node.ClassName == "android.widget.EditText") return node;
            for (int i = 0; i < node.ChildCount; i++)
            {
                var result = FindInputField(node.GetChild(i));
                if (result != null) return result;
            }
            return null;
        }

        private AccessibilityNodeInfo FindButton(AccessibilityNodeInfo node, string buttonText)
        {
            if (node == null) return null;
            if (node.Text?.ToString().Contains(buttonText) == true) return node;
            for (int i = 0; i < node.ChildCount; i++)
            {
                var result = FindButton(node.GetChild(i), buttonText);
                if (result != null) return result;
            }
            return null;
        }

        private string FlattenNodeText(AccessibilityNodeInfo node)
        {
            if (node == null) return "";
            string text = node.Text?.ToString() ?? "";
            for (int i = 0; i < node.ChildCount; i++)
            {
                text += " " + FlattenNodeText(node.GetChild(i));
            }
            return text;
        }

        public override void OnInterrupt() { }
    }
}