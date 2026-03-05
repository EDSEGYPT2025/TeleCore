using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using TeleCore.Mobile.Services;
using static TeleCore.Mobile.Services.SignalRService;


#if ANDROID
using Android.Content;
using Android.Provider;
#endif

namespace TeleCore.Mobile.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly IUssdService _ussdService;
        private readonly NetworkService _networkService; // 👈 تعريف الخدمة

        // 👇 حقن الخدمة هنا عشان الشاشة تعرف تستخدمها
        public MainPage(IUssdService ussdService, NetworkService networkService)
        {
            InitializeComponent();
            _ussdService = ussdService;
            _networkService = networkService; // 👈 ربط الخدمة

            // 1. استقبال أوامر الـ USSD من السيرفر
            WeakReferenceMessenger.Default.Register<RemoteOrderMessage>(this, (r, m) =>
            {
                var order = m.Value;
                string ussdCode = $"*9*7*{order.TargetNumber}*{order.Amount}#";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try { _ussdService.DialUssd(ussdCode, order.SimId); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[TeleCore] Error: {ex.Message}"); }
                });
            });

            // 2. استقبال تنبيه "تحديد الموقع" (Ping) من الموقع
            WeakReferenceMessenger.Default.Register<string, string>(this, "PingDevice", (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    Vibration.Default.Vibrate(TimeSpan.FromSeconds(2));
                    await DisplayAlert("تنبيه", "تم طلب فحص اتصال هذا الجهاز من الموقع", "موافق");
                });
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // فحص الصلاحيات
            await CheckAndRequestAllPermissionsAsync();

            // قراءة وعرض أرقام الشرائح المحفوظة في هذا الهاتف
            if (SimIdsEntry != null)
            {
                SimIdsEntry.Text = Microsoft.Maui.Storage.Preferences.Default.Get("MySimIds", "1,2");
            }

            // 👇 🚀 إطلاق شرارة الاتصال بالسيرفر!
            if (_networkService != null)
            {
                await _networkService.StartAndRegisterAsync();
            }

            // (باقي كود الـ SignalR القديم لو لسه بتستخدمه للواجهة)
            if (SignalRService.Instance != null)
            {
                UpdateConnectionStatus(SignalRService.Instance.IsConnected);
                if (!SignalRService.Instance.IsConnected)
                {
                    await SignalRService.Instance.ConnectAsync();
                    UpdateConnectionStatus(SignalRService.Instance.IsConnected);
                }
            }
        }

        // --- تحديث الواجهة (اللمبة والنص) ---
        private void UpdateConnectionStatus(bool isConnected)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                if (StatusDot != null && StatusLabel != null)
                {
                    StatusDot.Fill = isConnected ? Brush.Green : Brush.Red;
                    StatusLabel.Text = isConnected ? "متصل بالسيرفر" : "غير متصل - تأكد من الإنترنت";
                    StatusLabel.TextColor = isConnected ? Colors.Green : Colors.Red;
                }
            });
        }

        private async void OnReconnectClicked(object sender, EventArgs e)
        {
            UpdateConnectionStatus(false);
            StatusLabel.Text = "جاري محاولة الاتصال...";

            // إعادة المحاولة من الخدمة الجديدة
            if (_networkService != null) await _networkService.StartAndRegisterAsync();

            if (SignalRService.Instance != null)
            {
                await SignalRService.Instance.ConnectAsync();
                UpdateConnectionStatus(SignalRService.Instance.IsConnected);
            }
        }

        private async void OnSaveSimSettingsClicked(object sender, EventArgs e)
        {
            string simsInput = SimIdsEntry.Text;
            if (string.IsNullOrWhiteSpace(simsInput))
            {
                await DisplayAlert("تنبيه", "برجاء إدخال أرقام الشرائح (مثال: 1,2 أو 3)", "موافق");
                return;
            }

            try
            {
                UpdateConnectionStatus(false);
                StatusLabel.Text = "جاري تحديث الشرائح...";

                await SignalRService.Instance.SaveSimsAndReconnectAsync(simsInput);
                UpdateConnectionStatus(SignalRService.Instance.IsConnected);
                await DisplayAlert("نجاح", "تم حفظ أرقام الشرائح وإعادة الاتصال بالسيرفر بنجاح.", "موافق");
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "تأكد من كتابة الأرقام بشكل صحيح ومفصول بفاصلة (,)", "موافق");
            }
        }

        private async Task CheckAndRequestAllPermissionsAsync()
        {
            try
            {
                var smsStatus = await Permissions.CheckStatusAsync<Permissions.Sms>();
                if (smsStatus != PermissionStatus.Granted) await Permissions.RequestAsync<Permissions.Sms>();

                var phoneStatus = await Permissions.CheckStatusAsync<Permissions.Phone>();
                if (phoneStatus != PermissionStatus.Granted) await Permissions.RequestAsync<Permissions.Phone>();

#if ANDROID
                if (!IsAccessibilityServiceEnabled())
                {
                    bool userAgreed = await DisplayAlert("صلاحية هامة", "يرجى تفعيل خدمة (TeleCore) للأتمتة.", "فتح الإعدادات", "لاحقاً");
                    if (userAgreed)
                    {
                        var intent = new Intent(Settings.ActionAccessibilitySettings);
                        intent.AddFlags(ActivityFlags.NewTask);
                        Android.App.Application.Context.StartActivity(intent);
                    }
                }
#endif
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

#if ANDROID
        private bool IsAccessibilityServiceEnabled()
        {
            var context = Android.App.Application.Context;
            string enabledServices = Settings.Secure.GetString(context.ContentResolver, Settings.Secure.EnabledAccessibilityServices);
            return !string.IsNullOrEmpty(enabledServices) && enabledServices.Contains(context.PackageName);
        }
#endif

        private async void OnExecuteUssdClicked(object sender, EventArgs e) { }

        private async void OnHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HistoryPage());
        }

        private async void OnEnableAccessibilityClicked(object sender, EventArgs e)
        {
            bool goSettings = await DisplayAlert(
                "تفعيل الأتمتة الذكية",
                "للسماح للتطبيق بكتابة الـ PIN أوتوماتيكياً، يرجى تفعيل 'TeleCore Auto-PIN' من الإعدادات.",
                "فتح الإعدادات",
                "إلغاء");

            if (goSettings)
            {
#if ANDROID
                try
                {
                    var intent = new Intent(Settings.ActionAccessibilitySettings);
                    intent.AddFlags(ActivityFlags.NewTask);
                    Android.App.Application.Context.StartActivity(intent);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); }
#endif
            }
        }
    }
}