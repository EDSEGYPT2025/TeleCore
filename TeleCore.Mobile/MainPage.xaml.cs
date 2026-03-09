using TeleCore.Mobile.Services;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.Globalization;

#if ANDROID
using TeleCore.Mobile.Platforms.Android;
#endif

namespace TeleCore.Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly ApiService _apiService = new ApiService();
        private IDispatcherTimer _radarTimer;
        private ApiService.DeviceSyncResponse _lastSyncResult;

        public MainPage()
        {
            InitializeComponent();

            // 1. استقبال الأمر من اللاسلكي وفتح شاشة الاتصال
            WeakReferenceMessenger.Default.Register<NetworkService.RemoteOrderMessage>(this, (r, m) =>
            {
                HandleIncomingOrder(m.Value);
            });

            // 2. استقبال نتيجة الشاشة من خدمة الوصول (Accessibility)
            WeakReferenceMessenger.Default.Register<string>(this, async (r, screenText) =>
            {
                if (NetworkService.Instance.IsConnected)
                {
                    await NetworkService.Instance.SendResultToServerAsync(screenText);
                }
            });

            StartRadar();
        }

        private void HandleIncomingOrder(NetworkService.RemoteOrder order)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 📟 تجهيز كود الـ USSD
                string ussdCode = $"*9*7*{order.TargetNumber}*{order.Amount}#";

                // 🛰️ تحديد مكان الشريحة (Slot)
                string savedSims = Preferences.Default.Get("MySimIds", "");
                var simList = savedSims.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(int.Parse).ToList();
                int slotIndex = simList.IndexOf(order.SimId);
                if (slotIndex == -1) slotIndex = 0;

                Debug.WriteLine($"[MainPage] 🚀 تنفيذ التحويل للرقم {order.TargetNumber} من شريحة {order.SimId}");

                await DisplayAlert("[ NEW_ORDER ]", $"جاري تحويل {order.Amount} لـ {order.TargetNumber}", "تنفيذ");

                string savedPin = Preferences.Default.Get($"Pin_{order.SimId}", "");

#if ANDROID
                UssdAccessibilityService.CurrentDecryptedPin = savedPin;
                var ussdService = Handler.MauiContext.Services.GetService<IUssdService>();
                ussdService?.DialUssd(ussdCode, slotIndex + 1);
#endif
            });
        }

        private async void StartRadar()
        {
            await CheckRadarStatusAsync();
            _radarTimer = Dispatcher.CreateTimer();
            _radarTimer.Interval = TimeSpan.FromSeconds(15);
            _radarTimer.Tick += async (s, e) => await CheckRadarStatusAsync();
            _radarTimer.Start();
        }

        private async Task CheckRadarStatusAsync()
        {
            _lastSyncResult = await _apiService.SyncDeviceWithRadarAsync();
            if (_lastSyncResult != null && _lastSyncResult.Success)
            {
                if (_lastSyncResult.IsAuthorized)
                {
                    if (_lastSyncResult.AssignedSims != null && _lastSyncResult.AssignedSims.Any())
                    {
                        string simsDisplay = string.Join(" | ", _lastSyncResult.AssignedSims.Values);
                        BtnReconnect.Text = $"[ ACTIVE: {simsDisplay} ]";

                        List<int> simIds = _lastSyncResult.AssignedSims.Keys.ToList();
                        await NetworkService.Instance.SaveSimsAndReconnectAsync(simIds);
                    }
                }
            }
        }

        // 🎯 حل الخطأ MAUIX2002: دالة زر إعادة الاتصال
        private async void OnReconnectClicked(object sender, EventArgs e)
        {
            BtnReconnect.IsEnabled = false;
            BtnReconnect.Text = "جاري الاتصال...";
            await NetworkService.Instance.StartAsync();
            BtnReconnect.Text = "تم تحديث الاتصال";
            BtnReconnect.IsEnabled = true;
        }

        // 🎯 حل الخطأ MAUIX2002: دالة زر حفظ الإعدادات والـ PIN
        private async void OnSaveSimSettingsClicked(object sender, EventArgs e)
        {
            if (_lastSyncResult?.AssignedSims != null && _lastSyncResult.AssignedSims.Any())
            {
                foreach (var sim in _lastSyncResult.AssignedSims)
                {
                    string currentPin = Preferences.Default.Get($"Pin_{sim.Key}", "");
                    string result = await DisplayPromptAsync("تحديث الذخيرة", $"أدخل الرقم السري لمحفظة الشريحة: {sim.Value}", initialValue: currentPin, keyboard: Keyboard.Numeric);
                    if (!string.IsNullOrEmpty(result)) Preferences.Default.Set($"Pin_{sim.Key}", result);
                }
                await DisplayAlert("تم", "تم تسليح الشرائح بالأرقام السرية بنجاح!", "موافق");
            }
            else
            {
                await DisplayAlert("تنبيه", "الرادار لم يلتقط أي شرائح متصلة حالياً.", "موافق");
            }
        }

        // 🎯 حل الخطأ MAUIX2002: دالة زر تفعيل الـ Accessibility
        private void OnEnableAccessibilityClicked(object sender, EventArgs e)
        {
#if ANDROID
            try
            {
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionAccessibilitySettings);
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            catch { DisplayAlert("خطأ", "لا يمكن فتح الإعدادات تلقائياً", "موافق"); }
#else
            DisplayAlert("تنبيه", "هذه الميزة متاحة فقط على أجهزة أندرويد", "موافق");
#endif
        }
    }
}