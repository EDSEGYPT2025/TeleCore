using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace TeleCore.Mobile.Pages
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // تشغيل دوران الخلفية
            _ = AnimatedGradient.RotateTo(360, 10000, Easing.Linear);

            // أنيميشن اللوجو
            await Task.WhenAll(
                LogoImage.ScaleTo(1.15, 800, Easing.CubicOut),
                AppNameLabel.FadeTo(1, 1000, Easing.CubicOut)
            );
            await LogoImage.ScaleTo(1.0, 500, Easing.CubicIn);

            await LoadingLabel.FadeTo(1, 500);

            // 🚀 التعديل: استخدام الرادار الجديد NetworkService بدلاً من القديم المحذوف
            _ = Task.Run(async () => {
                try
                {
                    await TeleCore.Mobile.Services.NetworkService.Instance.StartAsync();
                }
                catch { /* نكتفي بتسجيل الخطأ في الـ Debug */ }
            });

            // انتظار بسيط عشان المستخدم يشوف اللوجو
            await Task.Delay(1000);

            // الانتقال للـ Shell
            if (Handler?.MauiContext != null)
            {
                var shell = Handler.MauiContext.Services.GetService<AppShell>();
                MainThread.BeginInvokeOnMainThread(() => {
                    Application.Current.Windows[0].Page = shell;
                });
            }
        }
    }
}