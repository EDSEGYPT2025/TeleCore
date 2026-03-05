using TeleCore.Mobile.Services;

namespace TeleCore.Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly NetworkService _networkService;

        public MainPage()
        {
            InitializeComponent();

            // تعريف خدمة الشبكة
            _networkService = new NetworkService();

            // محاولة الاتصال التلقائي عند فتح التطبيق
            StartConnection();
        }

        private async void StartConnection()
        {
            try
            {
                string deviceId = DeviceInfo.Name;
                await _networkService.StartAndRegisterAsync(deviceId);
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "فشل الاتصال الأولي بالسيرفر", "موافق");
            }
        }

        // 1. دالة إعادة الاتصال (المطلوبة في الخطأ الأول)
        private async void OnReconnectClicked(object sender, EventArgs e)
        {
            BtnReconnect.IsEnabled = false;
            BtnReconnect.Text = "جاري الاتصال...";

            await _networkService.StartAndRegisterAsync(DeviceInfo.Name);

            BtnReconnect.Text = "إعادة الاتصال بالسيرفر";
            BtnReconnect.IsEnabled = true;

            await DisplayAlert("تحديث", "تمت محاولة إعادة الاتصال", "موافق");
        }

        // 2. دالة حفظ الإعدادات (المطلوبة في الخطأ الثاني)
        private async void OnSaveSimSettingsClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EntPin.Text))
            {
                await DisplayAlert("تنبيه", "يرجى إدخال الرقم السري", "موافق");
                return;
            }

            // هنا يمكنك حفظ الرقم السري في Preferences لاستخدامه لاحقاً
            Preferences.Set("DefaultPin", EntPin.Text);
            await DisplayAlert("تم", "تم حفظ الإعدادات محلياً", "موافق");
        }

        // 3. دالة تفعيل الـ Accessibility (المطلوبة في الخطأ الثالث)
        private void OnEnableAccessibilityClicked(object sender, EventArgs e)
        {
#if ANDROID
            try
            {
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionAccessibilitySettings);
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                DisplayAlert("خطأ", "لا يمكن فتح الإعدادات تلقائياً", "موافق");
            }
#else
            DisplayAlert("تنبيه", "هذه الميزة متاحة فقط على أجهزة أندرويد", "موافق");
#endif
        }
    }
}